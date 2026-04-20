import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_state.dart';

class BillsListPage extends StatelessWidget {
  const BillsListPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        title: const Text('Bills'),
        backgroundColor: Colors.white,
        foregroundColor: AppColors.foreground,
        elevation: 0,
        shape: const Border(
          bottom: BorderSide(color: AppColors.surfaceVariant),
        ),
        actions: [
          IconButton(
            tooltip: 'Sync pending',
            icon: const Icon(Icons.cloud_sync_outlined),
            onPressed: () =>
                context.read<BillsListBloc>().add(const FlushAllRequested()),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        backgroundColor: AppColors.primary,
        icon: const Icon(Icons.add, color: Colors.white),
        label: const Text('New Order',
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.w600)),
        onPressed: () => context.goNamed('createBill'),
      ),
      body: BlocBuilder<BillsListBloc, BillsListState>(
        builder: (ctx, state) {
          if (state is BillsListLoading || state is BillsListInitial) {
            return const Center(child: CircularProgressIndicator());
          }
          if (state is BillsListError) {
            return Center(child: Text(state.message));
          }
          final loaded = state as BillsListLoaded;
          if (loaded.bills.isEmpty) {
            return const _EmptyView();
          }
          return ListView.separated(
            padding: const EdgeInsets.only(bottom: 96),
            itemCount: loaded.bills.length,
            separatorBuilder: (_, __) =>
                const Divider(height: 1, color: AppColors.surfaceVariant),
            itemBuilder: (_, i) => _BillTile(bill: loaded.bills[i]),
          );
        },
      ),
    );
  }
}

class _BillTile extends StatelessWidget {
  final Bill bill;
  const _BillTile({required this.bill});

  @override
  Widget build(BuildContext context) {
    final label = bill.syncStatus == SyncStatus.synced
        ? (bill.serverBillNumber ?? '—')
        : '#${bill.clientBillId.substring(0, 6)}';
    return ListTile(
      onTap: () =>
          context.goNamed('billDetail', pathParameters: {'id': bill.clientBillId}),
      title: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(fontWeight: FontWeight.w700),
            ),
          ),
          _StatusChip(status: bill.syncStatus),
        ],
      ),
      subtitle: Padding(
        padding: const EdgeInsets.only(top: 4),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              '${bill.items.length} items · Rs. ${bill.totalAmount.toStringAsFixed(2)}',
              style: const TextStyle(color: AppColors.foregroundMuted),
            ),
            Text(
              _formatDateTime(bill.createdAt),
              style: const TextStyle(
                color: AppColors.foregroundMuted,
                fontSize: 11,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final SyncStatus status;
  const _StatusChip({required this.status});

  @override
  Widget build(BuildContext context) {
    final (color, label, icon) = switch (status) {
      SyncStatus.synced => (AppColors.success, 'Synced', Icons.cloud_done),
      SyncStatus.syncing => (AppColors.primary, 'Syncing', Icons.cloud_upload),
      SyncStatus.pending => (AppColors.warning, 'Pending', Icons.schedule),
      SyncStatus.failed => (AppColors.error, 'Failed', Icons.error_outline),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.14),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: color.withValues(alpha: 0.4)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 12, color: color),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
                color: color, fontWeight: FontWeight.w600, fontSize: 11),
          ),
        ],
      ),
    );
  }
}

String _formatDateTime(DateTime d) {
  String two(int n) => n.toString().padLeft(2, '0');
  return '${d.year}-${two(d.month)}-${two(d.day)} '
      '${two(d.hour)}:${two(d.minute)}';
}

class _EmptyView extends StatelessWidget {
  const _EmptyView();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.receipt_long,
                size: 64, color: AppColors.foregroundMuted),
            const SizedBox(height: 12),
            const Text(
              'No bills yet',
              style:
                  TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 6),
            const Text(
              'Tap New Order to create your first bill.',
              style: TextStyle(color: AppColors.foregroundMuted),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}
