import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';
import 'package:uswatte/features/todays_route_map/domain/enums/route_outlet_status.dart';

class OutletMapSheet extends StatelessWidget {
  final RouteMapOutlet routeOutlet;

  const OutletMapSheet({super.key, required this.routeOutlet});

  @override
  Widget build(BuildContext context) {
    final outlet = routeOutlet.outlet;

    return Padding(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Center(
            child: Container(
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: AppColors.foregroundMuted.withValues(alpha: 0.3),
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          const SizedBox(height: 16),
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Expanded(
                child: Text(
                  outlet.name,
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w700,
                        color: AppColors.foreground,
                      ),
                ),
              ),
              const SizedBox(width: 12),
              _StatusBadge(status: routeOutlet.status),
            ],
          ),
          const SizedBox(height: 8),
          _InfoRow(icon: Icons.location_on_outlined, text: outlet.address),
          if (outlet.tel.isNotEmpty)
            _InfoRow(icon: Icons.phone_outlined, text: outlet.tel),
          if (outlet.contactPerson != null && outlet.contactPerson!.isNotEmpty)
            _InfoRow(icon: Icons.person_outline, text: outlet.contactPerson!),
          const SizedBox(height: 20),
          if (routeOutlet.status == RouteOutletStatus.pending)
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: AppColors.onPrimary,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10),
                  ),
                ),
                icon: const Icon(Icons.directions, size: 20),
                label: const Text('Open in Google Maps'),
                onPressed: () => _openDirections(outlet.latitude, outlet.longitude),
              ),
            ),
        ],
      ),
    );
  }

  Future<void> _openDirections(double lat, double lng) async {
    final uri = Uri.parse(
      'https://www.google.com/maps/dir/?api=1&destination=$lat,$lng&travelmode=driving',
    );
    if (await canLaunchUrl(uri)) {
      await launchUrl(uri, mode: LaunchMode.externalApplication);
    }
  }
}

class _StatusBadge extends StatelessWidget {
  final RouteOutletStatus status;
  const _StatusBadge({required this.status});

  @override
  Widget build(BuildContext context) {
    final (label, bg, fg) = switch (status) {
      RouteOutletStatus.billed => ('Billed', const Color(0xFFE8F5E9), const Color(0xFF2E7D32)),
      RouteOutletStatus.notBilled => ('Not Billed', const Color(0xFFFFF3E0), const Color(0xFFE65100)),
      RouteOutletStatus.pending => ('Pending', const Color(0xFFFFEBEE), const Color(0xFFC62828)),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(20)),
      child: Text(label, style: TextStyle(color: fg, fontSize: 12, fontWeight: FontWeight.w600)),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String text;
  const _InfoRow({required this.icon, required this.text});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 6),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 16, color: AppColors.foregroundMuted),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              text,
              style: Theme.of(context)
                  .textTheme
                  .bodyMedium
                  ?.copyWith(color: AppColors.foregroundMuted),
            ),
          ),
        ],
      ),
    );
  }
}
