import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/products/domain/entities/product.dart';
import 'package:uswatte/features/products/presentation/bloc/products_bloc.dart';
import 'package:uswatte/features/products/presentation/bloc/products_event.dart';
import 'package:uswatte/features/products/presentation/bloc/products_state.dart';

class ProductsPage extends StatefulWidget {
  const ProductsPage({super.key});

  @override
  State<ProductsPage> createState() => _ProductsPageState();
}

class _ProductsPageState extends State<ProductsPage> {
  final _searchController = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: AppColors.background,
      body: BlocBuilder<ProductsBloc, ProductsState>(
        builder: (context, state) {
          return CustomScrollView(
            slivers: [
              _AppBar(
                isSyncing: state is ProductsLoaded && state.isSyncing,
                lastSyncedAt:
                    state is ProductsLoaded ? state.lastSyncedAt : null,
                onSync: () =>
                    context.read<ProductsBloc>().add(const SyncProductsRequested()),
                onBack: () => context.pop(),
              ),
              SliverToBoxAdapter(
                child: _SearchBar(
                  controller: _searchController,
                  onChanged: (v) => setState(() => _query = v.trim().toLowerCase()),
                ),
              ),
              if (state is ProductsLoading)
                const SliverFillRemaining(child: _LoadingView())
              else if (state is ProductsError)
                SliverFillRemaining(child: _ErrorView(message: state.message))
              else if (state is ProductsLoaded)
                _ProductList(
                  products: state.products,
                  query: _query,
                )
              else
                const SliverFillRemaining(child: _LoadingView()),
            ],
          );
        },
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
    final now = DateTime.now();
    final diff = now.difference(lastSyncedAt!);
    if (diff.inMinutes < 1) return 'Synced just now';
    if (diff.inMinutes < 60) return 'Synced ${diff.inMinutes}m ago';
    if (diff.inHours < 24) return 'Synced ${diff.inHours}h ago';
    return 'Synced ${diff.inDays}d ago';
  }

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Container(
        decoration: BoxDecoration(
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
                // Back button
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
                        'PRODUCTS',
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
                // Sync button
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
          style: GoogleFonts.barlow(
              fontSize: 13.sp, color: AppColors.foreground),
          decoration: InputDecoration(
            hintText: 'Search by code or name…',
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

// ── Product list ──────────────────────────────────────────────────────────────

class _ProductList extends StatelessWidget {
  const _ProductList({required this.products, required this.query});

  final List<Product> products;
  final String query;

  List<Product> get _filtered {
    if (query.isEmpty) return products;
    return products
        .where((p) =>
            p.code.toLowerCase().contains(query) ||
            p.itemDescription.toLowerCase().contains(query))
        .toList();
  }

  @override
  Widget build(BuildContext context) {
    final items = _filtered;

    if (items.isEmpty) {
      return SliverFillRemaining(
        child: Center(
          child: Text(
            query.isEmpty ? 'No products yet.\nTap sync to load.' : 'No results for "$query"',
            textAlign: TextAlign.center,
            style: GoogleFonts.barlow(
                fontSize: 13.sp, color: AppColors.foregroundMuted),
          ),
        ),
      );
    }

    return SliverPadding(
      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 24.h),
      sliver: SliverList.separated(
        itemCount: items.length,
        separatorBuilder: (_, __) => SizedBox(height: 8.h),
        itemBuilder: (context, i) => _ProductTile(product: items[i]),
      ),
    );
  }
}

class _ProductTile extends StatelessWidget {
  const _ProductTile({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
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
          // Code badge
          Container(
            padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(8.r),
              border:
                  Border.all(color: AppColors.primary.withValues(alpha: 0.20)),
            ),
            child: Text(
              product.code,
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.5,
                color: AppColors.primary,
              ),
            ),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  product.itemDescription,
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w600,
                    color: AppColors.foreground,
                  ),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),
                if (product.printDescription != null &&
                    product.printDescription!.isNotEmpty) ...[
                  SizedBox(height: 2.h),
                  Text(
                    product.printDescription!,
                    style: GoogleFonts.barlow(
                      fontSize: 11.sp,
                      color: AppColors.foregroundMuted,
                    ),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ],
            ),
          ),
          SizedBox(width: 10.w),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                '${product.piecesPerPack}',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 22.sp,
                  fontWeight: FontWeight.w900,
                  height: 1.0,
                  letterSpacing: -0.5,
                  color: AppColors.foreground,
                ),
              ),
              Text(
                'PCS/PACK',
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

// ── Loading + Error views ─────────────────────────────────────────────────────

class _LoadingView extends StatelessWidget {
  const _LoadingView();

  @override
  Widget build(BuildContext context) {
    return const Center(child: AppSpinner());
  }
}

class _ErrorView extends StatelessWidget {
  const _ErrorView({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.wifi_off_rounded,
                size: 40.r, color: AppColors.foregroundMuted),
            SizedBox(height: 12.h),
            Text(
              message,
              textAlign: TextAlign.center,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foregroundMuted),
            ),
            SizedBox(height: 16.h),
            GestureDetector(
              onTap: () =>
                  context.read<ProductsBloc>().add(const LoadProductsRequested()),
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 20.w, vertical: 10.h),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(8.r),
                ),
                child: Text(
                  'RETRY',
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
