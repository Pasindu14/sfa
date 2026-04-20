import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';

/// Live product search. Debounced 300ms so we don't hit SQLite on every keystroke.
class ProductSearchField extends StatefulWidget {
  final bool enabled;
  final SearchProductsForBillUseCase searchUseCase;
  final ValueChanged<ProductWithPrice> onProductChosen;
  final int? pricingStructureId;

  const ProductSearchField({
    super.key,
    required this.enabled,
    required this.searchUseCase,
    required this.onProductChosen,
    this.pricingStructureId,
  });

  @override
  State<ProductSearchField> createState() => _ProductSearchFieldState();
}

class _ProductSearchFieldState extends State<ProductSearchField> {
  final TextEditingController _controller = TextEditingController();
  Timer? _debounce;
  List<ProductWithPrice> _results = [];
  bool _searching = false;

  @override
  void dispose() {
    _debounce?.cancel();
    _controller.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    if (value.trim().isEmpty) {
      setState(() => _results = []);
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 300), () async {
      setState(() => _searching = true);
      try {
        final results = await widget.searchUseCase(
          value,
          pricingStructureId: widget.pricingStructureId,
        );
        if (mounted) setState(() => _results = results);
      } finally {
        if (mounted) setState(() => _searching = false);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // ── Search input ───────────────────────────────────────────────
        Container(
          decoration: BoxDecoration(
            color: Colors.white,
            borderRadius: BorderRadius.circular(12.r),
            border: Border.all(color: AppColors.surfaceVariant),
            boxShadow: [
              BoxShadow(
                color: AppColors.foreground.withValues(alpha: 0.03),
                blurRadius: 8,
                offset: const Offset(0, 2),
              ),
            ],
          ),
          child: TextField(
            controller: _controller,
            enabled: widget.enabled,
            onChanged: _onChanged,
            style: GoogleFonts.barlow(
              fontSize: 14.sp,
              color: AppColors.foreground,
            ),
            decoration: InputDecoration(
              prefixIcon: Icon(
                Icons.search_rounded,
                size: 18.r,
                color: widget.enabled
                    ? AppColors.primary
                    : AppColors.foregroundMuted,
              ),
              hintText: widget.enabled
                  ? 'Search by product code or name'
                  : 'Select an outlet first',
              hintStyle: GoogleFonts.barlow(
                fontSize: 13.sp,
                color: AppColors.foregroundMuted,
              ),
              filled: false,
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12.r),
                borderSide: BorderSide.none,
              ),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12.r),
                borderSide: BorderSide.none,
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12.r),
                borderSide:
                    BorderSide(color: AppColors.primary, width: 1.5),
              ),
              disabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(12.r),
                borderSide: BorderSide.none,
              ),
              contentPadding:
                  EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
              suffixIcon: _searching
                  ? Padding(
                      padding: EdgeInsets.all(12.r),
                      child: SizedBox(
                        width: 14.r,
                        height: 14.r,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: AppColors.primary),
                      ),
                    )
                  : null,
            ),
          ),
        ),

        // ── Results dropdown ───────────────────────────────────────────
        if (_results.isNotEmpty) ...[
          SizedBox(height: 6.h),
          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              border: Border.all(color: AppColors.surfaceVariant),
              borderRadius: BorderRadius.circular(12.r),
              boxShadow: [
                BoxShadow(
                  color: AppColors.foreground.withValues(alpha: 0.06),
                  blurRadius: 12,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            constraints: BoxConstraints(maxHeight: 280.h),
            child: ClipRRect(
              borderRadius: BorderRadius.circular(12.r),
              child: ListView.separated(
                padding: EdgeInsets.zero,
                shrinkWrap: true,
                itemCount: _results.length,
                separatorBuilder: (_, __) => Divider(
                  height: 1,
                  color: AppColors.surfaceVariant,
                ),
                itemBuilder: (ctx, i) {
                  final r = _results[i];
                  return InkWell(
                    onTap: () {
                      widget.onProductChosen(r);
                      _controller.clear();
                      setState(() => _results = []);
                    },
                    child: Padding(
                      padding: EdgeInsets.symmetric(
                          horizontal: 14.w, vertical: 10.h),
                      child: Row(
                        children: [
                          Container(
                            width: 32.r,
                            height: 32.r,
                            decoration: BoxDecoration(
                              color: AppColors.primary.withValues(alpha: 0.08),
                              borderRadius: BorderRadius.circular(8.r),
                            ),
                            child: Icon(Icons.inventory_2_rounded,
                                size: 14.r, color: AppColors.primary),
                          ),
                          SizedBox(width: 10.w),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  r.itemDescription,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 14.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 0.2,
                                    color: AppColors.foreground,
                                  ),
                                ),
                                Text(
                                  r.code,
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
                            padding: EdgeInsets.symmetric(
                                horizontal: 8.w, vertical: 4.h),
                            decoration: BoxDecoration(
                              color: AppColors.primary.withValues(alpha: 0.08),
                              borderRadius: BorderRadius.circular(6.r),
                            ),
                            child: Text(
                              r.dealerPackPrice == null
                                  ? '—'
                                  : 'Rs. ${r.dealerPackPrice!.toStringAsFixed(0)}',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 13.sp,
                                fontWeight: FontWeight.w800,
                                color: AppColors.primary,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
          ),
        ],
      ],
    );
  }
}
