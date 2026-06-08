import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/stock/data/datasources/distributor_stock_local_datasource.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

const _kAccent = Color(0xFF7C3AED);

class StockCatalogPage extends StatefulWidget {
  const StockCatalogPage({super.key});

  @override
  State<StockCatalogPage> createState() => _StockCatalogPageState();
}

class _StockCatalogPageState extends State<StockCatalogPage> {
  final _searchController = TextEditingController();
  String _query = '';
  List<StockWithProduct> _items = [];
  DateTime? _lastSyncedAt;
  bool _loading = true;
  bool _syncing = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    final local = getIt<DistributorStockLocalDatasource>();
    final items = await local.getAllWithProductInfo();
    final lastSynced = await local.getLastSyncedAt();
    if (mounted) {
      setState(() {
        _items = items;
        _lastSyncedAt = lastSynced;
        _loading = false;
      });
    }
  }

  Future<void> _sync() async {
    if (_syncing) return;
    setState(() => _syncing = true);
    try {
      await getIt<SyncDistributorStockUseCase>()();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(e.toString()),
            backgroundColor: AppColors.warning,
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } finally {
      await _load();
      if (mounted) setState(() => _syncing = false);
    }
  }

  List<StockWithProduct> get _filtered {
    if (_query.isEmpty) return _items;
    final q = _query.toLowerCase();
    return _items
        .where((s) =>
            s.productCode.toLowerCase().contains(q) ||
            s.productName.toLowerCase().contains(q))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    final filtered = _filtered;

    return Scaffold(
      backgroundColor: AppColors.background,
      body: CustomScrollView(
        slivers: [
          _AppBar(
            isSyncing: _syncing,
            lastSyncedAt: _lastSyncedAt,
            onSync: _sync,
            onBack: () => context.pop(),
          ),
          SliverToBoxAdapter(
            child: _SearchBar(
              controller: _searchController,
              onChanged: (v) => setState(() => _query = v.trim()),
            ),
          ),
          if (_loading)
            const SliverFillRemaining(
              child: Center(child: AppSpinner()),
            )
          else if (_items.isEmpty)
            SliverFillRemaining(child: _EmptyView(onSync: _sync))
          else if (filtered.isEmpty)
            SliverFillRemaining(
              child: Center(
                child: Text(
                  'No results for "$_query"',
                  style: GoogleFonts.barlow(
                      fontSize: 13.sp, color: AppColors.foregroundMuted),
                ),
              ),
            )
          else
            _StockList(items: filtered),
        ],
      ),
    );
  }
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _AppBar extends StatelessWidget {
  const _AppBar({
    required this.isSyncing,
    required this.lastSyncedAt,
    required this.onSync,
    required this.onBack,
  });

  final bool isSyncing;
  final DateTime? lastSyncedAt;
  final VoidCallback onSync;
  final VoidCallback onBack;

  String _syncLabel() {
    if (isSyncing) return 'Syncing…';
    if (lastSyncedAt == null) return 'Never synced';
    final diff = DateTime.now().difference(lastSyncedAt!);
    if (diff.inMinutes < 1) return 'Synced just now';
    if (diff.inMinutes < 60) return 'Synced ${diff.inMinutes}m ago';
    if (diff.inHours < 24) return 'Synced ${diff.inHours}h ago';
    return 'Synced ${diff.inDays}d ago';
  }

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Container(
        decoration: const BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
            colors: [Color(0xFF5B21B6), _kAccent],
          ),
        ),
        child: SafeArea(
          bottom: false,
          child: Padding(
            padding: EdgeInsets.fromLTRB(8.w, 4.h, 8.w, 16.h),
            child: Row(
              children: [
                GestureDetector(
                  onTap: onBack,
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
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'DISTRIBUTOR STOCK',
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
                        _syncLabel(),
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                ),
                GestureDetector(
                  onTap: isSyncing ? null : onSync,
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
                    child: isSyncing
                        ? const Center(
                            child: AppSpinner.small(color: Colors.white))
                        : Icon(Icons.sync_rounded,
                            size: 16.r, color: Colors.white),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── Search bar ────────────────────────────────────────────────────────────────

class _SearchBar extends StatelessWidget {
  const _SearchBar({required this.controller, required this.onChanged});

  final TextEditingController controller;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 4.h, 16.w, 8.h),
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
          style:
              GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foreground),
          decoration: InputDecoration(
            hintText: 'Search by code or product name…',
            hintStyle: GoogleFonts.barlow(
                fontSize: 13.sp, color: AppColors.foregroundMuted),
            prefixIcon: Icon(Icons.search_rounded,
                size: 16.r, color: AppColors.foregroundMuted),
            border: InputBorder.none,
            contentPadding:
                EdgeInsets.symmetric(vertical: 10.h, horizontal: 4.w),
          ),
        ),
      ),
    );
  }
}

// ── Stock list ────────────────────────────────────────────────────────────────

class _StockList extends StatelessWidget {
  const _StockList({required this.items});

  final List<StockWithProduct> items;

  @override
  Widget build(BuildContext context) {
    return SliverPadding(
      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 24.h),
      sliver: SliverList.separated(
        itemCount: items.length,
        separatorBuilder: (_, __) => SizedBox(height: 8.h),
        itemBuilder: (_, i) => _StockTile(item: items[i]),
      ),
    );
  }
}

class _StockTile extends StatelessWidget {
  const _StockTile({required this.item});

  final StockWithProduct item;

  @override
  Widget build(BuildContext context) {
    final qty = item.quantityOnHand;
    final isZero = qty <= 0;
    final isFreeIssue = item.stockType == 'FreeIssue';

    final qtyColor = isZero ? AppColors.warning : AppColors.success;
    final typeColor = isFreeIssue ? AppColors.amber : _kAccent;

    return Container(
      padding: EdgeInsets.all(14.r),
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
      child: Row(
        children: [
          // Product code badge
          Container(
            padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
            decoration: BoxDecoration(
              color: _kAccent.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(8.r),
              border: Border.all(color: _kAccent.withValues(alpha: 0.20)),
            ),
            child: Text(
              item.productCode,
              style: GoogleFonts.barlowCondensed(
                fontSize: 12.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.5,
                color: _kAccent,
              ),
            ),
          ),
          SizedBox(width: 12.w),

          // Product name + stock type
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.productName,
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w600,
                    color: AppColors.foreground,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                SizedBox(height: 4.h),
                Container(
                  padding:
                      EdgeInsets.symmetric(horizontal: 6.w, vertical: 2.h),
                  decoration: BoxDecoration(
                    color: typeColor.withValues(alpha: 0.08),
                    borderRadius: BorderRadius.circular(4.r),
                    border:
                        Border.all(color: typeColor.withValues(alpha: 0.25)),
                  ),
                  child: Text(
                    isFreeIssue ? 'FREE ISSUE' : 'NORMAL',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.8,
                      color: typeColor,
                    ),
                  ),
                ),
              ],
            ),
          ),
          SizedBox(width: 10.w),

          // Quantity
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                qty % 1 == 0 ? qty.toInt().toString() : qty.toStringAsFixed(1),
                style: GoogleFonts.barlowCondensed(
                  fontSize: 24.sp,
                  fontWeight: FontWeight.w900,
                  height: 1.0,
                  letterSpacing: -0.5,
                  color: qtyColor,
                ),
              ),
              Text(
                'UNITS',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 8.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 1.0,
                  color: AppColors.foregroundMuted,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Empty view ────────────────────────────────────────────────────────────────

class _EmptyView extends StatelessWidget {
  const _EmptyView({required this.onSync});

  final VoidCallback onSync;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.warehouse_rounded,
                size: 40.r, color: AppColors.foregroundMuted),
            SizedBox(height: 12.h),
            Text(
              'No stock data yet.\nTap sync to load.',
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
            SizedBox(height: 16.h),
            GestureDetector(
              onTap: onSync,
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 20.w, vertical: 10.h),
                decoration: BoxDecoration(
                  color: _kAccent,
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
        ),
      ),
    );
  }
}
