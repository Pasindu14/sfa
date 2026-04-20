import 'package:flutter/material.dart';
import 'package:uswatte/core/theme/app_theme.dart';

/// Small pill showing how many bills are waiting to sync.
/// Renders nothing when [count] is zero so it never clutters the UI during a
/// clean state.
class PendingSyncBadge extends StatelessWidget {
  final int count;
  const PendingSyncBadge({super.key, required this.count});

  @override
  Widget build(BuildContext context) {
    if (count <= 0) return const SizedBox.shrink();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: AppColors.warning.withValues(alpha: 0.18),
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: AppColors.warning.withValues(alpha: 0.4)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_upload_outlined,
              size: 14, color: AppColors.warning),
          const SizedBox(width: 4),
          Text(
            '$count pending',
            style: const TextStyle(
              fontWeight: FontWeight.w600,
              color: AppColors.warning,
              fontSize: 12,
            ),
          ),
        ],
      ),
    );
  }
}
