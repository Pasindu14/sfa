import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/create_not_billing_bloc.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/create_not_billing_event.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/create_not_billing_state.dart';
import 'package:uswatte/features/not_billings/presentation/widgets/not_billing_reason_picker.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_state.dart';
import 'package:uswatte/features/bills/presentation/widgets/outlet_picker.dart';

class CreateNotBillingPage extends StatefulWidget {
  const CreateNotBillingPage({super.key});

  @override
  State<CreateNotBillingPage> createState() => _CreateNotBillingPageState();
}

class _CreateNotBillingPageState extends State<CreateNotBillingPage> {
  final _notesController = TextEditingController();
  Outlet? _selectedOutlet;

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocListener<CreateNotBillingBloc, CreateNotBillingState>(
      listenWhen: (prev, curr) =>
          curr.submittedClientId != null && prev.submittedClientId == null,
      listener: (context, state) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text(
            'Recorded — will sync when online',
            style: GoogleFonts.barlow(
                color: Colors.white,
                fontWeight: FontWeight.w500,
                fontSize: 13.sp),
          ),
          backgroundColor: AppColors.success,
          behavior: SnackBarBehavior.floating,
          margin: EdgeInsets.all(16.w),
          shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8.r)),
          duration: const Duration(seconds: 2),
        ));
        context.canPop() ? context.pop() : context.goNamed('notBillingsList');
      },
      child: Scaffold(
        backgroundColor: AppColors.background,
        body: Column(
          children: [
            // ── Header ─────────────────────────────────────────────────────
            Container(
              decoration: const BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [AppColors.primaryDark, AppColors.primary],
                ),
              ),
              child: SafeArea(
                bottom: false,
                child: Padding(
                  padding: EdgeInsets.fromLTRB(8.w, 4.h, 8.w, 16.h),
                  child: Row(
                    children: [
                      GestureDetector(
                        onTap: () => context.canPop() ? context.pop() : null,
                        child: Container(
                          width: 40.r,
                          height: 40.r,
                          margin: EdgeInsets.all(4.r),
                          decoration: BoxDecoration(
                            color: Colors.white.withValues(alpha: 0.15),
                            borderRadius: BorderRadius.circular(10.r),
                            border: Border.all(
                                color: Colors.white.withValues(alpha: 0.25)),
                          ),
                          child: Icon(Icons.arrow_back_ios_new_rounded,
                              size: 15.r, color: Colors.white),
                        ),
                      ),
                      SizedBox(width: 4.w),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'NOT BILLING',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 18.sp,
                                fontWeight: FontWeight.w800,
                                letterSpacing: 1.5,
                                height: 1.0,
                                color: Colors.white,
                              ),
                            ),
                            Text(
                              'Record a non-sale visit',
                              style: GoogleFonts.barlow(
                                fontSize: 11.sp,
                                color: Colors.white.withValues(alpha: 0.75),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),

            // ── Form ───────────────────────────────────────────────────────
            Expanded(
              child: BlocBuilder<CreateNotBillingBloc, CreateNotBillingState>(
                builder: (context, state) {
                  final outlets = context.watch<OutletsBloc>().state
                      is OutletsLoaded
                      ? (context.read<OutletsBloc>().state as OutletsLoaded)
                          .outlets
                      : <Outlet>[];

                  return SingleChildScrollView(
                    padding:
                        EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 120.h),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        // Outlet picker
                        OutletPicker(
                          selected: _selectedOutlet,
                          outlets: outlets,
                          onSelected: (outlet) {
                            setState(() => _selectedOutlet = outlet);
                            context.read<CreateNotBillingBloc>().add(
                                  OutletSelectedForNotBilling(
                                    outletId: outlet.id,
                                    outletName: outlet.name,
                                    routeName: outlet.routeName,
                                  ),
                                );
                          },
                        ),
                        SizedBox(height: 16.h),

                        // Reason picker
                        NotBillingReasonPicker(
                          selected: state.reason,
                          onSelected: (reason) {
                            context.read<CreateNotBillingBloc>().add(
                                  NotBillingReasonSelected(reason),
                                );
                          },
                        ),
                        SizedBox(height: 16.h),

                        // Notes field
                        Container(
                          decoration: BoxDecoration(
                            color: Colors.white,
                            borderRadius: BorderRadius.circular(14.r),
                            border: Border.all(color: AppColors.surfaceVariant),
                            boxShadow: [
                              BoxShadow(
                                color:
                                    AppColors.foreground.withValues(alpha: 0.04),
                                blurRadius: 10,
                                offset: const Offset(0, 3),
                              ),
                            ],
                          ),
                          child: TextField(
                            controller: _notesController,
                            maxLines: 4,
                            maxLength: 500,
                            onChanged: (v) => context
                                .read<CreateNotBillingBloc>()
                                .add(NotBillingNotesChanged(
                                    v.trim().isEmpty ? null : v)),
                            style: GoogleFonts.barlow(fontSize: 14.sp),
                            decoration: InputDecoration(
                              hintText: 'Additional details... (optional)',
                              hintStyle: GoogleFonts.barlow(
                                  fontSize: 13.sp,
                                  color: AppColors.foregroundMuted),
                              contentPadding: EdgeInsets.all(16.r),
                              border: InputBorder.none,
                              counterStyle: GoogleFonts.barlow(
                                  fontSize: 10.sp,
                                  color: AppColors.foregroundMuted),
                            ),
                          ),
                        ),

                        // Error message
                        if (state.errorMessage != null) ...[
                          SizedBox(height: 12.h),
                          Container(
                            padding: EdgeInsets.all(12.r),
                            decoration: BoxDecoration(
                              color: AppColors.error.withValues(alpha: 0.08),
                              borderRadius: BorderRadius.circular(10.r),
                              border: Border.all(
                                  color:
                                      AppColors.error.withValues(alpha: 0.3)),
                            ),
                            child: Row(
                              children: [
                                Icon(Icons.error_outline,
                                    size: 16.r, color: AppColors.error),
                                SizedBox(width: 8.w),
                                Expanded(
                                  child: Text(
                                    state.errorMessage!,
                                    style: GoogleFonts.barlow(
                                        fontSize: 12.sp,
                                        color: AppColors.error),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ],
                    ),
                  );
                },
              ),
            ),
          ],
        ),

        // ── Submit button ─────────────────────────────────────────────────
        floatingActionButtonLocation:
            FloatingActionButtonLocation.centerDocked,
        floatingActionButton: BlocBuilder<CreateNotBillingBloc,
            CreateNotBillingState>(
          builder: (context, state) {
            return Container(
              margin: EdgeInsets.fromLTRB(16.w, 0, 16.w, 24.h),
              child: SizedBox(
                width: double.infinity,
                height: 52.h,
                child: ElevatedButton(
                  onPressed: state.canSubmit
                      ? () => context
                          .read<CreateNotBillingBloc>()
                          .add(const SubmitNotBillingPressed())
                      : null,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    disabledBackgroundColor:
                        AppColors.surfaceVariant,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14.r),
                    ),
                    elevation: 0,
                  ),
                  child: state.submitting
                      ? const AppSpinner.button()
                      : Text(
                          'SUBMIT',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 16.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 2.0,
                            color: state.canSubmit
                                ? Colors.white
                                : AppColors.foregroundMuted,
                          ),
                        ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
