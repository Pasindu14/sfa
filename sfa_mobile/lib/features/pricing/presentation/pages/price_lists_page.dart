import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/utils/search_filter_cache.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/pricing/data/datasources/pricing_local_datasource.dart';
import 'package:uswatte/features/pricing/data/models/pricing_structure_model.dart';
import 'package:uswatte/features/pricing/domain/usecases/sync_pricing_structures_usecase.dart';

// Read-only viewer for the synced price lists, opened from the Sync page:
//   PriceListsPage       — every active price list, default first
//   PriceListDetailPage  — one list's products with pack / case / MRP prices
// Both read the local cache, so they work offline.

// ── Price lists ───────────────────────────────────────────────────────────────

class PriceListsPage extends StatefulWidget {
  const PriceListsPage({super.key});

  @override
  State<PriceListsPage> createState() => _PriceListsPageState();
}

class _PriceListsPageState extends State<PriceListsPage> {
  List<PricingStructureModel> _lists = [];
  DateTime? _lastSyncedAt;
  bool _loading = true;
  bool _syncing = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final local = getIt<PricingLocalDatasource>();
    final lists = await local.getAllStructures();
    final lastSynced = await local.getLastSyncedAt();
    if (!mounted) return;
    setState(() {
      _lists = lists;
      _lastSyncedAt = lastSynced;
      _loading = false;
    });
  }

  Future<void> _sync() async {
    if (_syncing) return;
    setState(() => _syncing = true);
    try {
      await getIt<SyncPricingStructuresUseCase>()(force: true);
    } catch (e) {
      if (mounted) _showError(context, e);
    } finally {
      await _load();
      if (mounted) setState(() => _syncing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: AppTheme.systemOverlayStyle,
      child: Scaffold(
        backgroundColor: AppColors.background,
        body: CustomScrollView(
          slivers: [
            _AppBar(
              title: 'PRICE LISTS',
              subtitle: _syncSubtitle(
                '${_lists.length} active list${_lists.length == 1 ? '' : 's'}',
                _lastSyncedAt,
                _syncing,
              ),
              isSyncing: _syncing,
              onSync: _sync,
              onBack: () => context.pop(),
            ),
            if (_loading)
              const SliverFillRemaining(child: Center(child: AppSpinner()))
            else if (_lists.isEmpty)
              SliverFillRemaining(
                child: _EmptyView(
                  message: 'No price lists yet.\nTap sync to load.',
                  onSync: _sync,
                ),
              )
            else
              SliverPadding(
                padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 24.h),
                sliver: SliverList.separated(
                  itemCount: _lists.length,
                  separatorBuilder: (_, __) => SizedBox(height: 10.h),
                  itemBuilder: (_, i) {
                    final list = _lists[i];
                    return _PriceListCard(
                      list: list,
                      onTap: () => context.push(
                        '/sales-rep/price-lists/${list.id}',
                        extra: list,
                      ),
                    );
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _PriceListCard extends StatelessWidget {
  const _PriceListCard({required this.list, required this.onTap});

  final PricingStructureModel list;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final accent = list.isDefault ? AppColors.primary : AppColors.foregroundMuted;

    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(12.r),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12.r),
        child: Container(
          padding: EdgeInsets.all(14.r),
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(12.r),
            border: Border.all(
              color: list.isDefault
                  ? AppColors.primary.withValues(alpha: 0.35)
                  : AppColors.surfaceVariant,
            ),
            boxShadow: [
              BoxShadow(
                color: AppColors.foreground.withValues(alpha: 0.04),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: Row(
            children: [
              Container(
                width: 44.r,
                height: 44.r,
                decoration: BoxDecoration(
                  color: accent.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Icon(
                  list.isDefault ? Icons.star_rounded : Icons.price_change_rounded,
                  size: 22.r,
                  color: list.isDefault ? AppColors.primary : AppColors.foregroundMuted,
                ),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      list.name,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.barlow(
                        fontSize: 15.sp,
                        fontWeight: FontWeight.w700,
                        color: AppColors.foreground,
                      ),
                    ),
                    SizedBox(height: 3.h),
                    Row(
                      children: [
                        if (list.isDefault) ...[
                          Container(
                            padding: EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
                            decoration: BoxDecoration(
                              color: AppColors.primary.withValues(alpha: 0.10),
                              borderRadius: BorderRadius.circular(4.r),
                            ),
                            child: Text(
                              'DEFAULT',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 0.8,
                                color: AppColors.primaryDark,
                              ),
                            ),
                          ),
                          SizedBox(width: 6.w),
                        ],
                        Text(
                          '${list.itemCount} product${list.itemCount == 1 ? '' : 's'}',
                          style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              Icon(Icons.chevron_right_rounded, size: 22.r, color: AppColors.foregroundMuted),
            ],
          ),
        ),
      ),
    );
  }
}

// ── One price list ────────────────────────────────────────────────────────────

class PriceListDetailPage extends StatefulWidget {
  const PriceListDetailPage({super.key, required this.structureId, this.initial});

  final int structureId;

  /// The list as shown on the previous page, so the header renders immediately.
  final PricingStructureModel? initial;

  @override
  State<PriceListDetailPage> createState() => _PriceListDetailPageState();
}

class _PriceListDetailPageState extends State<PriceListDetailPage> {
  final _searchController = TextEditingController();
  String _query = '';
  PricingStructureModel? _list;
  List<PriceListItem> _items = [];
  bool _loading = true;

  final _filter = SearchFilterCache<PriceListItem>((i) => [i.code, i.description]);

  @override
  void initState() {
    super.initState();
    _list = widget.initial;
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final local = getIt<PricingLocalDatasource>();
    final items = await local.getStructureItems(widget.structureId);
    final lists = await local.getAllStructures();
    if (!mounted) return;
    setState(() {
      _items = items;
      _list = lists.where((l) => l.id == widget.structureId).firstOrNull ?? _list;
      _loading = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    final visible = _filter.apply(_items, _query);
    final list = _list;

    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: AppTheme.systemOverlayStyle,
      child: Scaffold(
        backgroundColor: AppColors.background,
        body: CustomScrollView(
          slivers: [
            _AppBar(
              title: (list?.name ?? 'Price list').toUpperCase(),
              subtitle: [
                if (list?.isDefault ?? false) 'Default',
                '${_items.length} product${_items.length == 1 ? '' : 's'}',
              ].join(' · '),
              onBack: () => context.pop(),
            ),
            SliverToBoxAdapter(
              child: _SearchBar(
                controller: _searchController,
                onChanged: (v) => setState(() => _query = v.trim()),
              ),
            ),
            if (_loading)
              const SliverFillRemaining(child: Center(child: AppSpinner()))
            else if (_items.isEmpty)
              const SliverFillRemaining(
                child: _EmptyView(message: 'No products are priced in this list.'),
              )
            else if (visible.isEmpty)
              SliverFillRemaining(
                child: Center(
                  child: Text(
                    'No results for "$_query"',
                    style: GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted),
                  ),
                ),
              )
            else
              SliverPadding(
                padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 24.h),
                sliver: SliverList.separated(
                  itemCount: visible.length,
                  separatorBuilder: (_, __) => SizedBox(height: 8.h),
                  itemBuilder: (_, i) => _PriceItemTile(item: visible[i]),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _PriceItemTile extends StatelessWidget {
  const _PriceItemTile({required this.item});

  final PriceListItem item;

  @override
  Widget build(BuildContext context) {
    final casePrice = item.effectiveCasePrice;

    return Container(
      padding: EdgeInsets.fromLTRB(14.w, 12.h, 14.w, 12.h),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
        boxShadow: [
          BoxShadow(
            color: AppColors.foreground.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(6.r),
                  border: Border.all(color: AppColors.primary.withValues(alpha: 0.20)),
                ),
                child: Text(
                  item.code,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.5,
                    color: AppColors.primaryDark,
                  ),
                ),
              ),
              SizedBox(width: 10.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item.description,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        fontWeight: FontWeight.w600,
                        color: AppColors.foreground,
                      ),
                    ),
                    if (item.packsPerCase > 0)
                      Text(
                        '${item.packsPerCase} pkts / case',
                        style: GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.foregroundMuted),
                      ),
                  ],
                ),
              ),
            ],
          ),
          SizedBox(height: 10.h),
          Row(
            children: [
              Expanded(child: _PriceBox(label: 'PACK', value: item.packPrice, emphasis: true)),
              SizedBox(width: 8.w),
              Expanded(
                child: _PriceBox(
                  label: 'CASE',
                  value: casePrice,
                  // A blank case price is billed as pack × packs per case.
                  note: item.isCaseDerived && casePrice != null
                      ? 'pack × ${item.packsPerCase}'
                      : null,
                ),
              ),
              SizedBox(width: 8.w),
              Expanded(child: _PriceBox(label: 'MRP', value: item.mrp)),
            ],
          ),
        ],
      ),
    );
  }
}

class _PriceBox extends StatelessWidget {
  const _PriceBox({required this.label, required this.value, this.note, this.emphasis = false});

  final String label;
  final double? value;
  final String? note;
  final bool emphasis;

  @override
  Widget build(BuildContext context) {
    final muted = value == null || note != null;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 7.h),
      decoration: BoxDecoration(
        color: emphasis ? AppColors.primary.withValues(alpha: 0.06) : AppColors.surface,
        borderRadius: BorderRadius.circular(8.r),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 1.0,
              color: AppColors.foregroundMuted,
            ),
          ),
          SizedBox(height: 1.h),
          FittedBox(
            fit: BoxFit.scaleDown,
            alignment: Alignment.centerLeft,
            child: Text(
              value == null ? '—' : _money(value!),
              style: GoogleFonts.barlow(
                fontSize: 14.sp,
                fontWeight: emphasis ? FontWeight.w700 : FontWeight.w600,
                color: muted ? AppColors.foregroundMuted : AppColors.foreground,
                fontFeatures: const [FontFeature.tabularFigures()],
              ),
            ),
          ),
          if (note != null)
            Text(
              note!,
              style: GoogleFonts.barlow(fontSize: 9.sp, color: AppColors.foregroundMuted),
            ),
        ],
      ),
    );
  }
}

// ── Shared ────────────────────────────────────────────────────────────────────

/// 4593.6 → "4,593.60".
String _money(double v) {
  final fixed = v.toStringAsFixed(2);
  final dot = fixed.indexOf('.');
  final whole = fixed.substring(0, dot);
  final negative = whole.startsWith('-');
  final digits = negative ? whole.substring(1) : whole;
  final grouped = StringBuffer();
  for (var i = 0; i < digits.length; i++) {
    if (i > 0 && (digits.length - i) % 3 == 0) grouped.write(',');
    grouped.write(digits[i]);
  }
  return '${negative ? '-' : ''}$grouped${fixed.substring(dot)}';
}

String _syncSubtitle(String prefix, DateTime? lastSyncedAt, bool syncing) {
  if (syncing) return '$prefix · Syncing…';
  if (lastSyncedAt == null) return '$prefix · Never synced';
  final diff = DateTime.now().difference(lastSyncedAt);
  final ago = diff.inMinutes < 1
      ? 'just now'
      : diff.inMinutes < 60
          ? '${diff.inMinutes}m ago'
          : diff.inHours < 24
              ? '${diff.inHours}h ago'
              : '${diff.inDays}d ago';
  return '$prefix · Synced $ago';
}

void _showError(BuildContext context, Object e) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(e is AppException ? e.message : e.toString()),
      backgroundColor: AppColors.warning,
      behavior: SnackBarBehavior.floating,
    ),
  );
}

class _AppBar extends StatelessWidget {
  const _AppBar({
    required this.title,
    required this.subtitle,
    required this.onBack,
    this.isSyncing = false,
    this.onSync,
  });

  final String title;
  final String subtitle;
  final VoidCallback onBack;
  final bool isSyncing;

  /// Null hides the sync button.
  final VoidCallback? onSync;

  Widget _iconButton({required Widget child, VoidCallback? onTap}) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 40.r,
        height: 40.r,
        margin: EdgeInsets.all(4.r),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.15),
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(color: Colors.white.withValues(alpha: 0.25)),
        ),
        child: Center(child: child),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Container(
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
                _iconButton(
                  onTap: onBack,
                  child: Icon(Icons.arrow_back_ios_new_rounded, size: 15.r, color: Colors.white),
                ),
                SizedBox(width: 4.w),
                Expanded(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 2.r),
                      Text(
                        subtitle,
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.75),
                        ),
                      ),
                    ],
                  ),
                ),
                if (onSync != null)
                  _iconButton(
                    onTap: isSyncing ? null : onSync,
                    child: isSyncing
                        ? const AppSpinner.small(color: Colors.white)
                        : Icon(Icons.sync_rounded, size: 16.r, color: Colors.white),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _SearchBar extends StatelessWidget {
  const _SearchBar({required this.controller, required this.onChanged});

  final TextEditingController controller;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 8.h),
      child: Container(
        height: 40.h,
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(color: AppColors.surfaceVariant),
        ),
        child: TextField(
          controller: controller,
          onChanged: onChanged,
          style: GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foreground),
          decoration: InputDecoration(
            hintText: 'Search by code or product name…',
            hintStyle: GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted),
            prefixIcon: Icon(Icons.search_rounded, size: 16.r, color: AppColors.foregroundMuted),
            border: InputBorder.none,
            contentPadding: EdgeInsets.symmetric(vertical: 10.h, horizontal: 4.w),
          ),
        ),
      ),
    );
  }
}

class _EmptyView extends StatelessWidget {
  const _EmptyView({required this.message, this.onSync});

  final String message;
  final VoidCallback? onSync;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.price_change_rounded, size: 40.r, color: AppColors.foregroundMuted),
            SizedBox(height: 12.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
            if (onSync != null) ...[
              SizedBox(height: 16.h),
              GestureDetector(
                onTap: onSync,
                child: Container(
                  padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 10.h),
                  decoration: BoxDecoration(
                    color: AppColors.primary,
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Text(
                    'SYNC NOW',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.5,
                      color: Colors.white,
                    ),
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
