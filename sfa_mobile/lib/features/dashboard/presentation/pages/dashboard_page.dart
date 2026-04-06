import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/theme/app_theme.dart';

class DashboardPage extends StatelessWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.surface,
      body: CustomScrollView(
        slivers: [
          // ── App bar ───────────────────────────────────────────
          _DashboardAppBar(),

          // ── KPI card row ──────────────────────────────────────
          SliverToBoxAdapter(
            child: _KpiSection(),
          ),

          // ── Section: Today ────────────────────────────────────
          SliverToBoxAdapter(
            child: _SectionHeader(title: 'TODAY\'S ACTIVITY'),
          ),
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                _ActivityCard(
                  icon: Icons.receipt_long_rounded,
                  label: 'Orders Placed',
                  value: '—',
                  color: AppColors.primary,
                ),
                const SizedBox(height: 8),
                _ActivityCard(
                  icon: Icons.storefront_rounded,
                  label: 'Outlets Visited',
                  value: '—',
                  color: AppColors.primaryMedium,
                ),
                const SizedBox(height: 8),
                _ActivityCard(
                  icon: Icons.map_outlined,
                  label: 'Route Progress',
                  value: '—',
                  color: AppColors.amber,
                ),
                const SizedBox(height: 8),
              ]),
            ),
          ),

          // ── Section: Quick actions ────────────────────────────
          SliverToBoxAdapter(
            child: _SectionHeader(title: 'QUICK ACTIONS'),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 32),
            sliver: SliverGrid(
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                mainAxisSpacing: 10,
                crossAxisSpacing: 10,
                childAspectRatio: 1.55,
              ),
              delegate: SliverChildListDelegate([
                _QuickAction(
                  icon: Icons.add_shopping_cart_rounded,
                  label: 'New Order',
                  color: AppColors.primary,
                ),
                _QuickAction(
                  icon: Icons.location_on_rounded,
                  label: 'Check In',
                  color: AppColors.primaryMedium,
                ),
                _QuickAction(
                  icon: Icons.bar_chart_rounded,
                  label: 'My Sales',
                  color: AppColors.darkSurfaceCard,
                ),
                _QuickAction(
                  icon: Icons.inventory_2_rounded,
                  label: 'Products',
                  color: AppColors.darkSurface,
                ),
              ]),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Sliver App Bar ────────────────────────────────────────────────────────────
class _DashboardAppBar extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return SliverAppBar(
      expandedHeight: 160,
      pinned: true,
      backgroundColor: AppColors.darkSurface,
      automaticallyImplyLeading: false,
      actions: [
        IconButton(
          icon: const Icon(Icons.notifications_outlined, color: AppColors.onPrimary),
          onPressed: () {},
        ),
        IconButton(
          icon: Container(
            width: 32,
            height: 32,
            decoration: BoxDecoration(
              color: AppColors.primary,
              borderRadius: BorderRadius.circular(4),
            ),
            child: const Icon(Icons.logout_rounded,
                size: 16, color: AppColors.onPrimary),
          ),
          onPressed: () {
            // TODO: dispatch LogoutRequested event
          },
        ),
        const SizedBox(width: 8),
      ],
      flexibleSpace: FlexibleSpaceBar(
        titlePadding: const EdgeInsets.fromLTRB(20, 0, 0, 56),
        title: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'DASHBOARD',
              style: GoogleFonts.barlowCondensed(
                fontSize: 10,
                fontWeight: FontWeight.w700,
                letterSpacing: 3.0,
                color: AppColors.amber,
              ),
            ),
            Text(
              AppConstants.appName,
              style: GoogleFonts.barlowCondensed(
                fontSize: 20,
                fontWeight: FontWeight.w800,
                letterSpacing: 0.3,
                color: AppColors.onPrimary,
              ),
            ),
          ],
        ),
        background: Container(
          color: AppColors.darkSurface,
          child: Padding(
            padding: const EdgeInsets.fromLTRB(20, 60, 20, 0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SizedBox(height: 16),
                Text(
                  'Good morning,',
                  style: GoogleFonts.barlow(
                    fontSize: 13,
                    fontWeight: FontWeight.w400,
                    color: AppColors.onPrimary.withValues(alpha: 0.6),
                  ),
                ),
                Text(
                  'Sales Rep',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 26,
                    fontWeight: FontWeight.w700,
                    color: AppColors.onPrimary,
                    letterSpacing: 0.3,
                  ),
                ),
                const SizedBox(height: 12),
                // ── Orange accent bar ──
                Row(
                  children: [
                    Container(height: 2, width: 24, color: AppColors.primary),
                    const SizedBox(width: 4),
                    Container(height: 2, width: 8, color: AppColors.amber),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── KPI Section ───────────────────────────────────────────────────────────────
class _KpiSection extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      color: AppColors.background,
      padding: const EdgeInsets.fromLTRB(16, 20, 16, 20),
      child: Row(
        children: [
          _KpiTile(label: 'MTD SALES', value: '—', unit: 'LKR'),
          _KpiDivider(),
          _KpiTile(label: 'TARGET', value: '—', unit: 'LKR'),
          _KpiDivider(),
          _KpiTile(label: 'COVERAGE', value: '—', unit: '%'),
        ],
      ),
    );
  }
}

class _KpiTile extends StatelessWidget {
  const _KpiTile({
    required this.label,
    required this.value,
    required this.unit,
  });

  final String label;
  final String value;
  final String unit;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Text(
            label,
            style: GoogleFonts.barlowCondensed(
              fontSize: 10,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.5,
              color: AppColors.foregroundMuted,
            ),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: 4),
          Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 28,
              fontWeight: FontWeight.w800,
              letterSpacing: -0.5,
              color: AppColors.foreground,
              height: 1.0,
            ),
          ),
          Text(
            unit,
            style: GoogleFonts.barlowCondensed(
              fontSize: 10,
              fontWeight: FontWeight.w600,
              letterSpacing: 1.0,
              color: AppColors.primary,
            ),
          ),
        ],
      ),
    );
  }
}

class _KpiDivider extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      height: 40,
      width: 1,
      color: AppColors.surfaceVariant,
    );
  }
}

// ── Section header ────────────────────────────────────────────────────────────
class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title});

  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 20, 16, 10),
      child: Row(
        children: [
          Container(
            width: 3,
            height: 14,
            decoration: BoxDecoration(
              color: AppColors.primary,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            title,
            style: GoogleFonts.barlowCondensed(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              letterSpacing: 2.5,
              color: AppColors.foregroundMuted,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Activity card ─────────────────────────────────────────────────────────────
class _ActivityCard extends StatelessWidget {
  const _ActivityCard({
    required this.icon,
    required this.label,
    required this.value,
    required this.color,
  });

  final IconData icon;
  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.circular(6),
        border: Border.all(color: AppColors.surfaceVariant),
      ),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(4),
            ),
            child: Icon(icon, color: color, size: 20),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Text(
              label,
              style: GoogleFonts.barlowCondensed(
                fontSize: 15,
                fontWeight: FontWeight.w600,
                letterSpacing: 0.3,
                color: AppColors.foreground,
              ),
            ),
          ),
          Text(
            value,
            style: GoogleFonts.barlowCondensed(
              fontSize: 22,
              fontWeight: FontWeight.w800,
              color: color,
              letterSpacing: -0.5,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Quick action tile ─────────────────────────────────────────────────────────
class _QuickAction extends StatelessWidget {
  const _QuickAction({
    required this.icon,
    required this.label,
    required this.color,
  });

  final IconData icon;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: color,
      borderRadius: BorderRadius.circular(6),
      child: InkWell(
        onTap: () {},
        borderRadius: BorderRadius.circular(6),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Icon(icon, color: AppColors.onPrimary, size: 22),
              Text(
                label,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 15,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.3,
                  color: AppColors.onPrimary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
