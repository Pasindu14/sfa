import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/widgets/quantity_dialog.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';

/// Opens the product search screen with products grouped by category.
void showProductSearch(
  BuildContext context, {
  required SearchProductsForBillUseCase searchUseCase,
  required void Function(
    ProductWithPrice product,
    double qty,
    double unitPrice,
    double discountRate,
    String billingItemType,
    String? returnType,
    DateTime? expireDate,
  ) onProductAdded,
  int? pricingStructureId,
}) {
  Navigator.of(context).push(
    PageRouteBuilder<void>(
      opaque: false,
      transitionDuration: const Duration(milliseconds: 300),
      reverseTransitionDuration: const Duration(milliseconds: 220),
      pageBuilder: (_, __, ___) => _ProductSearchPage(
        searchUseCase: searchUseCase,
        onProductAdded: onProductAdded,
        pricingStructureId: pricingStructureId,
      ),
      transitionsBuilder: (_, animation, __, child) =>
          FadeTransition(opacity: animation, child: child),
    ),
  );
}

// ── Grouped list item sealed types ────────────────────────────────────────────

sealed class _ListItem {}

final class _Header extends _ListItem {
  final String label;
  final bool isExpanded;
  _Header(this.label, {required this.isExpanded});
}

final class _Product extends _ListItem {
  final ProductWithPrice product;
  _Product(this.product);
}

// ── Page ──────────────────────────────────────────────────────────────────────

class _ProductSearchPage extends StatefulWidget {
  final SearchProductsForBillUseCase searchUseCase;
  final int? pricingStructureId;
  final void Function(
    ProductWithPrice product,
    double qty,
    double unitPrice,
    double discountRate,
    String billingItemType,
    String? returnType,
    DateTime? expireDate,
  ) onProductAdded;

  const _ProductSearchPage({
    required this.searchUseCase,
    required this.onProductAdded,
    this.pricingStructureId,
  });

  @override
  State<_ProductSearchPage> createState() => _ProductSearchPageState();
}

class _ProductSearchPageState extends State<_ProductSearchPage> {
  final TextEditingController _controller = TextEditingController();
  String _query = '';
  Future<List<ProductWithPrice>>? _searchFuture;
  final Set<String> _expandedCategories = {};

  @override
  void initState() {
    super.initState();
    _searchFuture = _search('');
    WidgetsBinding.instance.addPostFrameCallback((_) {
      SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
        statusBarColor: Colors.transparent,
        statusBarIconBrightness: Brightness.light,
      ));
      // Sync categories in the background so grouping is always fresh.
      // After sync, re-run the current query to pick up new category names.
      getIt<SyncProductCategoriesUseCase>()().then((_) {
        if (mounted) setState(() => _searchFuture = _search(_query));
      }).ignore();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<List<ProductWithPrice>> _search(String q) => widget.searchUseCase(
        q,
        pricingStructureId: widget.pricingStructureId,
      );

  void _onQueryChanged(String value) {
    final trimmed = value.trim();
    if (trimmed == _query) return;
    setState(() {
      _query = trimmed;
      _searchFuture = _search(trimmed);
    });
  }

  Future<void> _pickProduct(ProductWithPrice product) async {
    final result = await showQuantityDialog(context, product: product);
    if (result != null && result.quantity > 0 && mounted) {
      widget.onProductAdded(
        product,
        result.quantity,
        result.unitPrice,
        result.discountRate,
        result.billingItemType,
        result.returnType,
        result.expireDate,
      );
      _controller.clear();
      setState(() {
        _query = '';
        _searchFuture = _search('');
      });
    }
  }

  void _toggleCategory(String label) {
    setState(() {
      if (_expandedCategories.contains(label)) {
        _expandedCategories.remove(label);
      } else {
        _expandedCategories.add(label);
      }
    });
  }

  /// Converts a flat sorted list into grouped [_ListItem]s with [_Header]s.
  /// SQL already orders by category name (nulls last) then product code,
  /// so we just scan for boundary changes.
  /// Products under collapsed headers are omitted from the list.
  List<_ListItem> _buildGrouped(List<ProductWithPrice> products) {
    final items = <_ListItem>[];
    String? lastLabel;
    for (final p in products) {
      final label = p.categoryName ?? 'Uncategorized';
      if (label != lastLabel) {
        items.add(_Header(
          label,
          isExpanded: _expandedCategories.contains(label),
        ));
        lastLabel = label;
      }
      if (_expandedCategories.contains(label)) {
        items.add(_Product(p));
      }
    }
    return items;
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: Column(
        children: [
          // ── Gradient header ─────────────────────────────────────────────────
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
                      onTap: () => Navigator.of(context).pop(),
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
                    Text(
                      'ADD PRODUCTS',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 18.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1.5,
                        height: 1.0,
                        color: Colors.white,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

          // ── Search bar ──────────────────────────────────────────────────────
          Padding(
            padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 4.h),
            child: Container(
              height: 40.h,
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(10.r),
                border: Border.all(color: AppColors.surfaceVariant),
              ),
              child: TextField(
                controller: _controller,
                autofocus: false,
                onChanged: _onQueryChanged,
                style: GoogleFonts.barlow(
                    fontSize: 13.sp, color: AppColors.foreground),
                decoration: InputDecoration(
                  hintText: 'Search by code or name…',
                  hintStyle: GoogleFonts.barlow(
                      fontSize: 13.sp, color: AppColors.foregroundMuted),
                  prefixIcon: Icon(Icons.search_rounded,
                      size: 16.r, color: AppColors.foregroundMuted),
                  suffixIcon: _controller.text.isNotEmpty
                      ? IconButton(
                          icon: Icon(Icons.clear_rounded,
                              size: 16.r, color: AppColors.foregroundMuted),
                          onPressed: () {
                            _controller.clear();
                            _onQueryChanged('');
                          },
                        )
                      : null,
                  border: InputBorder.none,
                  contentPadding: EdgeInsets.symmetric(
                      vertical: 10.h, horizontal: 4.w),
                ),
              ),
            ),
          ),

          // ── Grouped product list ────────────────────────────────────────────
          Expanded(
            child: FutureBuilder<List<ProductWithPrice>>(
              future: _searchFuture,
              builder: (ctx, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const Center(
                    child: CircularProgressIndicator(
                      strokeWidth: 2,
                      color: AppColors.primary,
                    ),
                  );
                }
                final results = snapshot.data ?? [];
                if (results.isEmpty) {
                  return _Prompt(
                    icon: Icons.inventory_2_outlined,
                    message: _query.isEmpty
                        ? 'No products available'
                        : 'No products matched "$_query"',
                  );
                }

                final items = _buildGrouped(results);
                return ListView.builder(
                  padding: EdgeInsets.only(bottom: 16.h),
                  itemCount: items.length,
                  itemBuilder: (_, i) {
                    final item = items[i];
                    return switch (item) {
                      _Header h => _CategoryHeader(
                          label: h.label,
                          isExpanded: h.isExpanded,
                          onTap: () => _toggleCategory(h.label),
                        ),
                      _Product p => _ProductTile(
                          product: p.product,
                          onTap: () => _pickProduct(p.product),
                        ),
                    };
                  },
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── Category section header ───────────────────────────────────────────────────

class _CategoryHeader extends StatelessWidget {
  const _CategoryHeader({
    required this.label,
    required this.isExpanded,
    required this.onTap,
  });

  final String label;
  final bool isExpanded;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isUncategorized = label == 'Uncategorized';
    final accentColor =
        isExpanded ? AppColors.primary : AppColors.foregroundMuted;

    return InkWell(
      onTap: onTap,
      child: Container(
        margin: EdgeInsets.only(top: 6.h),
        decoration: BoxDecoration(
          color: AppColors.surface,
          border: Border(
            left: BorderSide(
              color: accentColor.withValues(alpha: isExpanded ? 1.0 : 0.35),
              width: 3.w,
            ),
          ),
        ),
        padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 13.h),
        child: Row(
          children: [
            Container(
              width: 34.r,
              height: 34.r,
              decoration: BoxDecoration(
                color: accentColor.withValues(alpha: 0.10),
                borderRadius: BorderRadius.circular(9.r),
              ),
              child: Icon(
                isUncategorized
                    ? Icons.label_off_rounded
                    : Icons.label_rounded,
                size: 17.r,
                color: accentColor,
              ),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Text(
                label.toUpperCase(),
                style: GoogleFonts.barlowCondensed(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 1.4,
                  color: isExpanded
                      ? AppColors.foreground
                      : AppColors.foregroundMuted,
                ),
              ),
            ),
            AnimatedRotation(
              turns: isExpanded ? 0.0 : -0.25,
              duration: const Duration(milliseconds: 200),
              child: Icon(
                Icons.expand_more_rounded,
                size: 20.r,
                color: accentColor,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Product result tile ───────────────────────────────────────────────────────

class _ProductTile extends StatelessWidget {
  const _ProductTile({required this.product, required this.onTap});

  final ProductWithPrice product;
  final VoidCallback onTap;

  bool get _hasPrice =>
      product.dealerPackPrice != null && product.dealerPackPrice! > 0;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        InkWell(
          onTap: _hasPrice ? onTap : null,
          child: Opacity(
            opacity: _hasPrice ? 1.0 : 0.45,
            child: Padding(
              padding: EdgeInsets.symmetric(horizontal: 16.w, vertical: 10.h),
              child: Row(
                children: [
                  Container(
                    width: 36.r,
                    height: 36.r,
                    decoration: BoxDecoration(
                      color: _hasPrice
                          ? AppColors.primary.withValues(alpha: 0.08)
                          : AppColors.surfaceVariant,
                      borderRadius: BorderRadius.circular(9.r),
                    ),
                    child: Icon(Icons.inventory_2_rounded,
                        size: 16.r,
                        color: _hasPrice
                            ? AppColors.primary
                            : AppColors.foregroundMuted),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          product.itemDescription,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 15.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 0.2,
                            color: AppColors.foreground,
                          ),
                        ),
                        Text(
                          product.code,
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
                    ),
                  ),
                  SizedBox(width: 8.w),
                  Container(
                    padding:
                        EdgeInsets.symmetric(horizontal: 10.w, vertical: 4.h),
                    decoration: BoxDecoration(
                      color: _hasPrice
                          ? AppColors.primary.withValues(alpha: 0.08)
                          : AppColors.surfaceVariant,
                      borderRadius: BorderRadius.circular(8.r),
                    ),
                    child: Text(
                      _hasPrice
                          ? 'Rs. ${product.dealerPackPrice!.toStringAsFixed(0)}'
                          : 'No price',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 14.sp,
                        fontWeight: FontWeight.w800,
                        color: _hasPrice
                            ? AppColors.primary
                            : AppColors.foregroundMuted,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
        Divider(height: 1, color: AppColors.surfaceVariant),
      ],
    );
  }
}

// ── Empty / no-results prompt ─────────────────────────────────────────────────

class _Prompt extends StatelessWidget {
  const _Prompt({required this.icon, required this.message});
  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 40.r, color: AppColors.surfaceVariant),
          SizedBox(height: 12.h),
          Text(
            message,
            style: GoogleFonts.barlow(
              fontSize: 13.sp,
              color: AppColors.foregroundMuted,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
