import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
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
    String? freeIssueSource,
    DateTime? expireDate,
  ) onProductAdded,
}) {
  Navigator.of(context).push(
    PageRouteBuilder<void>(
      opaque: false,
      transitionDuration: const Duration(milliseconds: 300),
      reverseTransitionDuration: const Duration(milliseconds: 220),
      pageBuilder: (_, __, ___) => _ProductSearchPage(
        searchUseCase: searchUseCase,
        onProductAdded: onProductAdded,
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
  final int productCount;
  _Header(this.label, {required this.isExpanded, required this.productCount});
}

final class _Product extends _ListItem {
  final ProductWithPrice product;
  final bool isLast;
  _Product(this.product, {this.isLast = false});
}

// ── Page ──────────────────────────────────────────────────────────────────────

class _ProductSearchPage extends StatefulWidget {
  final SearchProductsForBillUseCase searchUseCase;
  final void Function(
    ProductWithPrice product,
    double qty,
    double unitPrice,
    double discountRate,
    String billingItemType,
    String? returnType,
    String? freeIssueSource,
    DateTime? expireDate,
  ) onProductAdded;

  const _ProductSearchPage({
    required this.searchUseCase,
    required this.onProductAdded,
  });

  @override
  State<_ProductSearchPage> createState() => _ProductSearchPageState();
}

class _ProductSearchPageState extends State<_ProductSearchPage> {
  final TextEditingController _controller = TextEditingController();
  final FocusNode _focusNode = FocusNode();
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
      getIt<SyncProductCategoriesUseCase>()().then((_) {
        if (mounted) setState(() => _searchFuture = _search(_query));
      }).ignore();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  Future<List<ProductWithPrice>> _search(String q) =>
      widget.searchUseCase(q);

  void _onQueryChanged(String value) {
    final trimmed = value.trim();
    if (trimmed == _query) return;
    setState(() {
      _query = trimmed;
      _searchFuture = _search(trimmed);
      // Auto-expand all categories when searching so results are visible.
      if (trimmed.isNotEmpty) {
        _expandedCategories.clear();
        _expandedCategories.add('__auto_expand__');
      }
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
        result.freeIssueSource,
        result.expireDate,
      );
      _controller.clear();
      setState(() {
        _query = '';
        _searchFuture = _search('');
        _expandedCategories.remove('__auto_expand__');
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

  /// Converts a flat sorted list into grouped [_ListItem]s with category counts.
  List<_ListItem> _buildGrouped(List<ProductWithPrice> products) {
    final items = <_ListItem>[];
    final isSearching = _query.isNotEmpty;

    // Count products per category first
    final Map<String, List<ProductWithPrice>> grouped = {};
    for (final p in products) {
      final label = p.categoryName ?? 'Uncategorized';
      grouped.putIfAbsent(label, () => []).add(p);
    }

    for (final entry in grouped.entries) {
      final label = entry.key;
      final categoryProducts = entry.value;
      final isExpanded = isSearching || _expandedCategories.contains(label);

      items.add(_Header(
        label,
        isExpanded: isExpanded,
        productCount: categoryProducts.length,
      ));

      if (isExpanded) {
        for (var i = 0; i < categoryProducts.length; i++) {
          items.add(_Product(
            categoryProducts[i],
            isLast: i == categoryProducts.length - 1,
          ));
        }
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
          // ── Dark gradient header ──────────────────────────────────────────────
          Container(
            decoration: const BoxDecoration(
              gradient: LinearGradient(
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
                colors: [AppColors.primaryDark, AppColors.primaryMedium, AppColors.primary],
                stops: [0.0, 0.5, 1.0],
              ),
            ),
            child: SafeArea(
              bottom: false,
              child: Column(
                children: [
                  // Title row
                  Padding(
                    padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 12.h),
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
                        SizedBox(width: 6.w),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(
                                'ADD PRODUCTS',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 20.sp,
                                  fontWeight: FontWeight.w800,
                                  letterSpacing: 2.0,
                                  height: 1.0,
                                  color: Colors.white,
                                ),
                              ),
                              Text(
                                'Tap a category to browse',
                                style: GoogleFonts.barlow(
                                  fontSize: 11.sp,
                                  color: Colors.white.withValues(alpha: 0.65),
                                ),
                              ),
                            ],
                          ),
                        ),
                        // Product icon accent
                        Container(
                          width: 38.r,
                          height: 38.r,
                          decoration: BoxDecoration(
                            color: Colors.white.withValues(alpha: 0.12),
                            borderRadius: BorderRadius.circular(10.r),
                          ),
                          child: Icon(Icons.inventory_2_rounded,
                              size: 18.r,
                              color: Colors.white.withValues(alpha: 0.85)),
                        ),
                      ],
                    ),
                  ),

                  // Search bar inset into the header for visual continuity
                  Padding(
                    padding: EdgeInsets.fromLTRB(12.w, 0, 12.w, 14.h),
                    child: AnimatedBuilder(
                      animation: _focusNode,
                      builder: (_, child) => Container(
                        height: 42.h,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12.r),
                          border: Border.all(
                            color: _focusNode.hasFocus
                                ? AppColors.amber
                                : Colors.transparent,
                            width: 2,
                          ),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black.withValues(alpha: 0.15),
                              blurRadius: 12,
                              offset: const Offset(0, 3),
                            ),
                          ],
                        ),
                        child: child,
                      ),
                      child: TextField(
                        controller: _controller,
                        focusNode: _focusNode,
                        autofocus: false,
                        onChanged: _onQueryChanged,
                        style: GoogleFonts.barlow(
                            fontSize: 13.sp, color: AppColors.foreground),
                        decoration: InputDecoration(
                          hintText: 'Search by code or name…',
                          hintStyle: GoogleFonts.barlow(
                              fontSize: 13.sp,
                              color: AppColors.foregroundMuted),
                          prefixIcon: Icon(Icons.search_rounded,
                              size: 18.r, color: AppColors.primary),
                          suffixIcon: _controller.text.isNotEmpty
                              ? IconButton(
                                  icon: Icon(Icons.clear_rounded,
                                      size: 16.r,
                                      color: AppColors.foregroundMuted),
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
                ],
              ),
            ),
          ),

          // ── Grouped product list ──────────────────────────────────────────────
          Expanded(
            child: FutureBuilder<List<ProductWithPrice>>(
              future: _searchFuture,
              builder: (ctx, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const AppSpinner(),
                        SizedBox(height: 12.h),
                        Text(
                          'Loading products…',
                          style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
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
                  padding: EdgeInsets.only(top: 8.h, bottom: 20.h),
                  itemCount: items.length,
                  itemBuilder: (_, i) {
                    final item = items[i];
                    return switch (item) {
                      _Header h => _CategoryHeader(
                          label: h.label,
                          isExpanded: h.isExpanded,
                          productCount: h.productCount,
                          onTap: () => _toggleCategory(h.label),
                        ),
                      _Product p => _ProductTile(
                          product: p.product,
                          isLast: p.isLast,
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
    required this.productCount,
    required this.onTap,
  });

  final String label;
  final bool isExpanded;
  final int productCount;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final isUncategorized = label == 'Uncategorized';

    // Collapsed: light primary tint card with orange left border.
    // Expanded:  solid primary orange fill with white text.
    final radius = BorderRadius.only(
      topLeft: Radius.circular(12.r),
      topRight: Radius.circular(12.r),
      bottomLeft: Radius.circular(isExpanded ? 0 : 12.r),
      bottomRight: Radius.circular(isExpanded ? 0 : 12.r),
    );

    return Padding(
      padding: EdgeInsets.fromLTRB(12.w, 8.h, 12.w, 0),
      child: Material(
        color: isExpanded ? AppColors.primary : AppColors.primary.withValues(alpha: 0.10),
        borderRadius: radius,
        child: InkWell(
          onTap: onTap,
          borderRadius: radius,
          splashColor: Colors.white.withValues(alpha: 0.15),
          highlightColor: Colors.white.withValues(alpha: 0.08),
          child: Container(
            decoration: BoxDecoration(
              borderRadius: radius,
              border: isExpanded
                  ? null
                  : Border.all(
                      color: AppColors.primary.withValues(alpha: 0.35),
                      width: 1.5,
                    ),
            ),
            padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 12.h),
            child: Row(
              children: [
                // Category icon box
                Container(
                  width: 36.r,
                  height: 36.r,
                  decoration: BoxDecoration(
                    color: isExpanded
                        ? Colors.white.withValues(alpha: 0.20)
                        : AppColors.primary.withValues(alpha: 0.18),
                    borderRadius: BorderRadius.circular(9.r),
                  ),
                  child: Icon(
                    isUncategorized
                        ? Icons.label_off_rounded
                        : Icons.category_rounded,
                    size: 17.r,
                    color: isExpanded
                        ? Colors.white
                        : AppColors.primaryDark,
                  ),
                ),
                SizedBox(width: 12.w),

                // Label
                Expanded(
                  child: Text(
                    label,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.4,
                      color: isExpanded
                          ? Colors.white
                          : AppColors.primaryDark,
                    ),
                  ),
                ),

                // Product count badge
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                  decoration: BoxDecoration(
                    color: isExpanded
                        ? Colors.white.withValues(alpha: 0.25)
                        : AppColors.primary,
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Text(
                    '$productCount',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 12.sp,
                      fontWeight: FontWeight.w800,
                      color: Colors.white,
                    ),
                  ),
                ),
                SizedBox(width: 8.w),

                // Expand/collapse chevron
                AnimatedRotation(
                  turns: isExpanded ? 0.0 : -0.25,
                  duration: const Duration(milliseconds: 200),
                  child: Icon(
                    Icons.expand_more_rounded,
                    size: 20.r,
                    color: isExpanded
                        ? Colors.white
                        : AppColors.primaryDark,
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

// ── Product result tile ───────────────────────────────────────────────────────

class _ProductTile extends StatelessWidget {
  const _ProductTile({
    required this.product,
    required this.onTap,
    this.isLast = false,
  });

  final ProductWithPrice product;
  final VoidCallback onTap;
  final bool isLast;

  bool get _hasPrice =>
      product.dealerPackPrice != null && product.dealerPackPrice! > 0;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: 12.w),
      child: Container(
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.only(
            bottomLeft: Radius.circular(isLast ? 12.r : 0),
            bottomRight: Radius.circular(isLast ? 12.r : 0),
          ),
          border: Border(
            left: BorderSide(color: AppColors.surfaceVariant, width: 1),
            right: BorderSide(color: AppColors.surfaceVariant, width: 1),
            bottom: BorderSide(
              color: isLast
                  ? AppColors.surfaceVariant
                  : AppColors.surfaceVariant.withValues(alpha: 0.5),
              width: 1,
            ),
          ),
        ),
        child: Material(
          color: Colors.transparent,
          child: InkWell(
            onTap: _hasPrice ? onTap : null,
            borderRadius: BorderRadius.only(
              bottomLeft: Radius.circular(isLast ? 12.r : 0),
              bottomRight: Radius.circular(isLast ? 12.r : 0),
            ),
            child: Opacity(
              opacity: _hasPrice ? 1.0 : 0.45,
              child: Padding(
                padding:
                    EdgeInsets.symmetric(horizontal: 14.w, vertical: 11.h),
                child: Row(
                  children: [
                    // Product icon
                    Container(
                      width: 38.r,
                      height: 38.r,
                      decoration: BoxDecoration(
                        color: _hasPrice
                            ? AppColors.primary.withValues(alpha: 0.08)
                            : AppColors.surfaceVariant,
                        borderRadius: BorderRadius.circular(9.r),
                      ),
                      child: Icon(Icons.inventory_2_rounded,
                          size: 17.r,
                          color: _hasPrice
                              ? AppColors.primary
                              : AppColors.foregroundMuted),
                    ),
                    SizedBox(width: 12.w),

                    // Name + code + stock badge
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
                          SizedBox(height: 1.h),
                          Row(
                            children: [
                              Text(
                                product.code,
                                style: GoogleFonts.barlow(
                                  fontSize: 10.sp,
                                  fontWeight: FontWeight.w500,
                                  color: AppColors.foregroundMuted,
                                ),
                              ),
                              if (product.normalStock != null) ...[
                                SizedBox(width: 6.w),
                                _StockBadge(qty: product.normalStock!),
                              ],
                            ],
                          ),
                        ],
                      ),
                    ),
                    SizedBox(width: 10.w),

                    // Price badge
                    Container(
                      padding: EdgeInsets.symmetric(
                          horizontal: 10.w, vertical: 5.h),
                      decoration: BoxDecoration(
                        color: _hasPrice
                            ? AppColors.primaryDark
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
                          letterSpacing: 0.3,
                          color: _hasPrice
                              ? Colors.white
                              : AppColors.foregroundMuted,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Stock availability badge ──────────────────────────────────────────────────

class _StockBadge extends StatelessWidget {
  const _StockBadge({required this.qty});
  final double qty;

  @override
  Widget build(BuildContext context) {
    final hasStock = qty > 0;
    final color = hasStock ? AppColors.success : AppColors.warning;
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 5.w, vertical: 1.5.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(4.r),
        border: Border.all(color: color.withValues(alpha: 0.30)),
      ),
      child: Text(
        'Stk: ${qty.toStringAsFixed(0)}',
        style: GoogleFonts.barlow(
          fontSize: 9.sp,
          fontWeight: FontWeight.w700,
          color: color,
          letterSpacing: 0.2,
        ),
      ),
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
          Container(
            width: 64.r,
            height: 64.r,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.08),
              shape: BoxShape.circle,
            ),
            child: Icon(icon, size: 30.r, color: AppColors.primary.withValues(alpha: 0.50)),
          ),
          SizedBox(height: 16.h),
          Text(
            message,
            style: GoogleFonts.barlowCondensed(
              fontSize: 15.sp,
              fontWeight: FontWeight.w600,
              color: AppColors.foregroundMuted,
            ),
            textAlign: TextAlign.center,
          ),
        ],
      ),
    );
  }
}
