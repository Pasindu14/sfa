import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/widgets/quantity_dialog.dart';

/// Full-screen product search using Flutter's built-in [showSearch] route.
///
/// Stays open for multiple adds — the rep closes it with the back arrow.
/// Each tap opens a [QuantityDialog]; on confirm the [onProductAdded] callback
/// fires directly into the BLoC (passed in as a closure from the page).
class ProductSearchDelegate extends SearchDelegate<void> {
  final SearchProductsForBillUseCase searchUseCase;
  final int? pricingStructureId;
  final void Function(ProductWithPrice product, double qty) onProductAdded;

  // Cache the last future so rapid rebuilds of buildSuggestions don't each
  // launch a separate SQLite query while the user is still typing.
  String? _lastQuery; // null = never fetched yet; fires immediately on first build
  Future<List<ProductWithPrice>>? _pendingSearch;

  ProductSearchDelegate({
    required this.searchUseCase,
    required this.onProductAdded,
    this.pricingStructureId,
  }) : super(
          searchFieldLabel: 'Search by code or name…',
          searchFieldStyle: GoogleFonts.barlow(fontSize: 15),
        );

  // ── AppBar actions ──────────────────────────────────────────────────────────

  @override
  List<Widget> buildActions(BuildContext context) => [
        if (query.isNotEmpty)
          IconButton(
            icon: const Icon(Icons.clear_rounded),
            onPressed: () {
              query = '';
              showSuggestions(context);
            },
          ),
      ];

  @override
  Widget buildLeading(BuildContext context) => IconButton(
        icon: Icon(Icons.arrow_back_ios_new_rounded, size: 18.r),
        onPressed: () => close(context, null),
      );

  // ── Results / Suggestions ───────────────────────────────────────────────────

  @override
  Widget buildResults(BuildContext context) => _buildList(context);

  @override
  Widget buildSuggestions(BuildContext context) => _buildList(context);

  Widget _buildList(BuildContext context) {
    final q = query.trim();

    // Re-use the cached future if the query hasn't changed.
    // Empty query intentionally passes through — the datasource uses
    // LIKE '%%' which matches all products, so the list is pre-populated.
    if (q != _lastQuery) {
      _lastQuery = q;
      _pendingSearch = searchUseCase(
        q,
        pricingStructureId: pricingStructureId,
        limit: 200,
      );
    }

    return FutureBuilder<List<ProductWithPrice>>(
      future: _pendingSearch,
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
            message: 'No products matched "$q"',
          );
        }
        return ListView.separated(
          padding: EdgeInsets.symmetric(vertical: 8.h),
          itemCount: results.length,
          separatorBuilder: (_, __) =>
              Divider(height: 1, color: AppColors.surfaceVariant),
          itemBuilder: (_, i) => _ProductTile(
            product: results[i],
            onTap: () => _pickProduct(context, results[i]),
          ),
        );
      },
    );
  }

  Future<void> _pickProduct(
      BuildContext context, ProductWithPrice product) async {
    final qty = await showQuantityDialog(context, product: product);
    if (qty != null && qty > 0 && context.mounted) {
      onProductAdded(product, qty);
      // Clear the field so the rep can immediately search the next product.
      query = '';
      showSuggestions(context);
    }
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
