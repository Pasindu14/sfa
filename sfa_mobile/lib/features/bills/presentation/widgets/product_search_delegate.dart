import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/widgets/quantity_dialog.dart';

/// Opens the product search screen with a fade transition.
/// Keyboard stays hidden on open — autofocus is disabled.
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

  @override
  void initState() {
    super.initState();
    _searchFuture = _search('');
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<List<ProductWithPrice>> _search(String q) => widget.searchUseCase(
        q,
        pricingStructureId: widget.pricingStructureId,
        limit: 200,
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

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

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

          // ── Product list ────────────────────────────────────────────────────
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
                return ListView.separated(
                  padding: EdgeInsets.symmetric(vertical: 8.h),
                  itemCount: results.length,
                  separatorBuilder: (_, __) =>
                      Divider(height: 1, color: AppColors.surfaceVariant),
                  itemBuilder: (_, i) => _ProductTile(
                    product: results[i],
                    onTap: () => _pickProduct(results[i]),
                  ),
                );
              },
            ),
          ),
        ],
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
    return InkWell(
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
