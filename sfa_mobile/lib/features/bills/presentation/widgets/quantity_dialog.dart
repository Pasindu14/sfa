import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';

class QuantityDialogResult {
  final double quantity;
  final double unitPrice;
  final double discountRate;

  const QuantityDialogResult({
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
  });
}

enum _UnitType { cases, packets }

Future<QuantityDialogResult?> showQuantityDialog(
  BuildContext context, {
  required ProductWithPrice product,
}) {
  return showModalBottomSheet<QuantityDialogResult>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (ctx) => _QuantitySheet(product: product),
  );
}

class _QuantitySheet extends StatefulWidget {
  final ProductWithPrice product;
  const _QuantitySheet({required this.product});

  @override
  State<_QuantitySheet> createState() => _QuantitySheetState();
}

class _QuantitySheetState extends State<_QuantitySheet> {
  _UnitType _unitType = _UnitType.packets;
  final TextEditingController _qtyController =
      TextEditingController(text: '1');
  final TextEditingController _discController =
      TextEditingController(text: '0');
  String? _qtyError;
  String? _discError;

  @override
  void dispose() {
    _qtyController.dispose();
    _discController.dispose();
    super.dispose();
  }

  bool get _hasCasesOption => widget.product.packsPerCase > 1;

  double get _packPrice => widget.product.dealerPackPrice ?? 0.0;
  int get _packsPerCase => widget.product.packsPerCase;

  double get _enteredQty =>
      double.tryParse(_qtyController.text.trim()) ?? 0;
  double get _enteredDisc =>
      double.tryParse(_discController.text.trim()) ?? 0;

  double get _qtyInPacks =>
      _unitType == _UnitType.cases ? _enteredQty * _packsPerCase : _enteredQty;

  double get _lineTotal {
    final gross = _qtyInPacks * _packPrice;
    return gross * (1 - _enteredDisc / 100);
  }

  void _submit() {
    final qty = double.tryParse(_qtyController.text.trim());
    final disc = double.tryParse(_discController.text.trim());

    bool hasError = false;
    if (qty == null || qty <= 0) {
      setState(() => _qtyError = 'Enter a quantity greater than zero.');
      hasError = true;
    } else {
      setState(() => _qtyError = null);
    }
    if (disc == null || disc < 0 || disc > 100) {
      setState(() => _discError = 'Enter a value between 0 and 100.');
      hasError = true;
    } else {
      setState(() => _discError = null);
    }
    if (hasError) return;

    final finalQty = _unitType == _UnitType.cases
        ? qty! * _packsPerCase
        : qty!;

    Navigator.of(context).pop(QuantityDialogResult(
      quantity: finalQty,
      unitPrice: _packPrice,
      discountRate: disc!,
    ));
  }

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.of(context).viewInsets.bottom;
    return Container(
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
      ),
      padding: EdgeInsets.only(bottom: bottom),
      child: SingleChildScrollView(
        child: Padding(
          padding: EdgeInsets.fromLTRB(20.w, 12.h, 20.w, 24.h),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                  width: 36.w,
                  height: 4.h,
                  decoration: BoxDecoration(
                    color: AppColors.surfaceVariant,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
              ),
              SizedBox(height: 16.h),
              Text(
                widget.product.itemDescription,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 20.sp,
                  fontWeight: FontWeight.w700,
                  color: AppColors.foreground,
                ),
              ),
              SizedBox(height: 6.h),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                decoration: BoxDecoration(
                  color: AppColors.surfaceVariant,
                  borderRadius: BorderRadius.circular(4.r),
                ),
                child: Text(
                  widget.product.code,
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    color: AppColors.foregroundMuted,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              SizedBox(height: 20.h),
              if (_hasCasesOption) ...[
                Text(
                  'UNIT TYPE',
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    color: AppColors.foregroundMuted,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 0.8,
                  ),
                ),
                SizedBox(height: 8.h),
                Row(
                  children: [
                    Expanded(
                      child: _UnitButton(
                        label: 'Cases',
                        icon: Icons.inventory_2_rounded,
                        selected: _unitType == _UnitType.cases,
                        onTap: () =>
                            setState(() => _unitType = _UnitType.cases),
                      ),
                    ),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: _UnitButton(
                        label: 'Packets',
                        icon: Icons.local_mall_rounded,
                        selected: _unitType == _UnitType.packets,
                        onTap: () =>
                            setState(() => _unitType = _UnitType.packets),
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 16.h),
              ],
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  RichText(
                    text: TextSpan(
                      children: [
                        TextSpan(
                          text: 'Rs. ${_packPrice.toStringAsFixed(2)}',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 20.sp,
                            fontWeight: FontWeight.w800,
                            color: AppColors.primary,
                          ),
                        ),
                        TextSpan(
                          text: ' / pack',
                          style: GoogleFonts.barlow(
                            fontSize: 13.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              if (_unitType == _UnitType.cases && _hasCasesOption) ...[
                SizedBox(height: 2.h),
                Text(
                  '1 case = $_packsPerCase packs',
                  style: GoogleFonts.barlow(
                    fontSize: 12.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
              SizedBox(height: 20.h),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: 3,
                    child: TextField(
                      controller: _qtyController,
                      autofocus: true,
                      keyboardType: const TextInputType.numberWithOptions(
                          decimal: true),
                      inputFormatters: [
                        FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                      ],
                      decoration: InputDecoration(
                        labelText: 'Quantity',
                        hintText:
                            _unitType == _UnitType.cases ? '# cases' : '# packs',
                        errorText: _qtyError,
                      ),
                      onChanged: (_) => setState(() {}),
                      onSubmitted: (_) => _submit(),
                    ),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    flex: 2,
                    child: TextField(
                      controller: _discController,
                      keyboardType: const TextInputType.numberWithOptions(
                          decimal: true),
                      inputFormatters: [
                        FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                      ],
                      decoration: InputDecoration(
                        labelText: 'Discount',
                        hintText: '0',
                        suffixText: '%',
                        errorText: _discError,
                      ),
                      onChanged: (_) => setState(() {}),
                      onSubmitted: (_) => _submit(),
                    ),
                  ),
                ],
              ),
              if (_unitType == _UnitType.cases &&
                  _enteredQty > 0 &&
                  _hasCasesOption) ...[
                SizedBox(height: 6.h),
                Text(
                  '= ${(_enteredQty * _packsPerCase).toStringAsFixed(0)} packs total',
                  style: GoogleFonts.barlow(
                    fontSize: 12.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
              SizedBox(height: 16.h),
              Container(
                width: double.infinity,
                padding:
                    EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
                decoration: BoxDecoration(
                  color: AppColors.surface,
                  borderRadius: BorderRadius.circular(8.r),
                  border: Border.all(color: AppColors.surfaceVariant),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Line Total',
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                    Text(
                      'Rs. ${_lineTotal.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w800,
                        color: _enteredDisc > 0
                            ? AppColors.success
                            : AppColors.foreground,
                      ),
                    ),
                  ],
                ),
              ),
              SizedBox(height: 20.h),
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(context).pop(),
                      child: const Text('Cancel'),
                    ),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    flex: 2,
                    child: FilledButton(
                      onPressed: _submit,
                      child: const Text('Add to Cart'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _UnitButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  const _UnitButton({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: EdgeInsets.symmetric(vertical: 11.h),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : AppColors.surface,
          borderRadius: BorderRadius.circular(8.r),
          border: Border.all(
            color: selected ? AppColors.primary : AppColors.surfaceVariant,
            width: 1.5,
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              icon,
              size: 15.r,
              color: selected ? AppColors.onPrimary : AppColors.foregroundMuted,
            ),
            SizedBox(width: 6.w),
            Text(
              label,
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
                fontWeight: FontWeight.w600,
                color: selected ? AppColors.onPrimary : AppColors.foreground,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
