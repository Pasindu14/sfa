import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/not_billings/data/datasources/not_billings_local_datasource.dart';

/// Rep logout that first offers to upload anything still in the outbox.
///
/// Unsynced rows survive a plain logout and upload when the same rep signs
/// back in, but if a *different* user signs in on this phone they are cleared
/// (see DeviceUserGuard) — so give the rep a chance to push them first.
Future<void> logoutWithSyncCheck(BuildContext context) async {
  final authBloc = context.read<AuthBloc>();
  void logout() => authBloc.add(const LogoutRequested());

  var pending = await _countUnsynced();
  if (pending.isEmpty) return logout();
  if (!context.mounted) return;

  final syncFirst = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text('Unsynced records',
          style: GoogleFonts.barlowCondensed(
              fontSize: 18.sp, fontWeight: FontWeight.w700)),
      content: Text(
        '${pending.describe()} not uploaded yet. Sync now before logging out?',
        style: GoogleFonts.barlow(fontSize: 13.sp),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(ctx).pop(false),
          child: const Text('Cancel'),
        ),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: AppColors.primary),
          onPressed: () => Navigator.of(ctx).pop(true),
          child: const Text('Sync & log out'),
        ),
      ],
    ),
  );
  if (syncFirst != true || !context.mounted) return;

  showDialog<void>(
    context: context,
    barrierDismissible: false,
    builder: (_) => PopScope(
      canPop: false,
      child: AlertDialog(
        content: Row(
          children: [
            SizedBox(
              width: 20.r,
              height: 20.r,
              child: const CircularProgressIndicator(
                  strokeWidth: 2.4, color: AppColors.primary),
            ),
            SizedBox(width: 16.w),
            Text('Syncing…', style: GoogleFonts.barlow(fontSize: 14.sp)),
          ],
        ),
      ),
    ),
  );
  try {
    await Future.wait([
      getIt<BillSyncService>().flushAll(force: true),
      getIt<NotBillingSyncService>().flushAll(force: true),
    ]).timeout(const Duration(seconds: 60));
  } catch (_) {
    // Offline / timeout / server error — the recount below tells the story.
  }
  pending = await _countUnsynced();
  if (!context.mounted) return;
  Navigator.of(context, rootNavigator: true).pop(); // progress dialog

  if (pending.isEmpty) return logout();

  final logoutAnyway = await showDialog<bool>(
    context: context,
    builder: (ctx) => AlertDialog(
      title: Text("Couldn't sync everything",
          style: GoogleFonts.barlowCondensed(
              fontSize: 18.sp, fontWeight: FontWeight.w700)),
      content: Text(
        '${pending.describe()} still not uploaded. They stay on this phone '
        'and upload when you log back in — but if a different user logs in '
        'on this phone, they will be deleted.',
        style: GoogleFonts.barlow(fontSize: 13.sp),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(ctx).pop(false),
          child: const Text('Stay logged in'),
        ),
        FilledButton(
          style: FilledButton.styleFrom(backgroundColor: AppColors.error),
          onPressed: () => Navigator.of(ctx).pop(true),
          child: const Text('Log out anyway'),
        ),
      ],
    ),
  );
  if (logoutAnyway == true) logout();
}

Future<_Unsynced> _countUnsynced() async {
  try {
    final counts = await Future.wait([
      getIt<BillsLocalDatasource>().countPendingOrFailed(),
      getIt<NotBillingsLocalDatasource>().countPendingOrFailed(),
    ]);
    return _Unsynced(counts[0], counts[1]);
  } catch (_) {
    // Can't read the outbox — don't trap the rep on the dashboard.
    return const _Unsynced(0, 0);
  }
}

class _Unsynced {
  final int bills;
  final int notBillings;
  const _Unsynced(this.bills, this.notBillings);

  bool get isEmpty => bills == 0 && notBillings == 0;

  String describe() {
    String n(int c, String one, String many) => '$c ${c == 1 ? one : many}';
    final parts = [
      if (bills > 0) n(bills, 'bill', 'bills'),
      if (notBillings > 0) n(notBillings, 'not-billing', 'not-billings'),
    ];
    final verb = (bills + notBillings) == 1 ? 'is' : 'are';
    return '${parts.join(' and ')} $verb';
  }
}
