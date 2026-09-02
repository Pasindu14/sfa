import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';

/// Final confirmation step before a bill is closed (SFA-120).
///
/// Shows the net total one last time and lets the rep record anything special
/// about the outlet — "shop closed early", "owner asked to deliver the balance
/// next visit". The remark lands on the bill header's `notes` field.
///
/// Returns `null` if the rep backed out (nothing is submitted, the cart stays
/// intact), or the remark text — possibly empty — if they confirmed. An empty
/// remark is normalised to `null` when the bill is built.
///
/// Deliberately a sheet rather than a field inside the cart panel: `CartList`
/// hides itself whenever the keyboard is up, so an inline field would vanish
/// the moment it was focused.
Future<String?> showConfirmBillSheet(
  BuildContext context, {
  required CreateBillState state,
}) {
  return showModalBottomSheet<String>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (ctx) => _ConfirmBillSheet(state: state),
  );
}

class _ConfirmBillSheet extends StatefulWidget {
  final CreateBillState state;
  const _ConfirmBillSheet({required this.state});

  @override
  State<_ConfirmBillSheet> createState() => _ConfirmBillSheetState();
}

class _ConfirmBillSheetState extends State<_ConfirmBillSheet> {
  static const int _maxRemarksLength = 500;

  final TextEditingController _remarksController = TextEditingController();

  @override
  void dispose() {
    _remarksController.dispose();
    super.dispose();
  }

  void _confirm() {
    Navigator.of(context).pop(_remarksController.text.trim());
  }

  @override
  Widget build(BuildContext context) {
    final state = widget.state;
    // Lift the sheet above the keyboard instead of letting it cover the field.
    final bottom = MediaQuery.of(context).viewInsets.bottom;

    return ScrollConfiguration(
      behavior: ScrollConfiguration.of(context).copyWith(overscroll: false),
      child: Container(
        decoration: BoxDecoration(
          color: AppColors.background,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: EdgeInsets.only(top: 10.h),
              child: Container(
                width: 36.w,
                height: 4.h,
                decoration: BoxDecoration(
                  color: AppColors.surfaceVariant,
                  borderRadius: BorderRadius.circular(2.r),
                ),
              ),
            ),
            Flexible(
              child: SingleChildScrollView(
                physics: const ClampingScrollPhysics(),
                keyboardDismissBehavior:
                    ScrollViewKeyboardDismissBehavior.onDrag,
                padding: EdgeInsets.fromLTRB(
                    20.w, 16.h, 20.w, bottom > 0 ? bottom + 16.h : 28.h),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Confirm Order',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 22.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.1,
                        color: AppColors.foreground,
                      ),
                    ),
                    if (state.outlet != null) ...[
                      SizedBox(height: 2.h),
                      Text(
                        state.outlet!.name,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.barlow(
                          fontSize: 13.sp,
                          color: AppColors.foregroundMuted,
                        ),
                      ),
                    ],

                    SizedBox(height: 14.h),
                    Divider(color: AppColors.surfaceVariant, height: 1),
                    SizedBox(height: 10.h),

                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(
                          'NET TOTAL',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 12.sp,
                            fontWeight: FontWeight.w600,
                            letterSpacing: 1.2,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                        Text(
                          'Rs. ${state.total.toStringAsFixed(2)}',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 19.sp,
                            fontWeight: FontWeight.w800,
                            color: AppColors.primaryDark,
                          ),
                        ),
                      ],
                    ),

                    SizedBox(height: 10.h),
                    Divider(color: AppColors.surfaceVariant, height: 1),
                    SizedBox(height: 14.h),

                    Text(
                      'REMARKS (OPTIONAL)',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w600,
                        letterSpacing: 1.2,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                    SizedBox(height: 6.h),
                    Container(
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(14.r),
                        border: Border.all(color: AppColors.surfaceVariant),
                      ),
                      child: TextField(
                        controller: _remarksController,
                        maxLines: 4,
                        maxLength: _maxRemarksLength,
                        textCapitalization: TextCapitalization.sentences,
                        style: GoogleFonts.barlow(fontSize: 14.sp),
                        decoration: InputDecoration(
                          hintText:
                              'Anything special about this outlet? (optional)',
                          hintStyle: GoogleFonts.barlow(
                            fontSize: 13.sp,
                            color: AppColors.foregroundMuted,
                          ),
                          contentPadding: EdgeInsets.all(16.r),
                          border: InputBorder.none,
                          counterStyle: GoogleFonts.barlow(
                            fontSize: 10.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ),
                    ),

                    SizedBox(height: 14.h),

                    Row(
                      children: [
                        Expanded(
                          child: TextButton(
                            onPressed: () => Navigator.of(context).pop(),
                            style: TextButton.styleFrom(
                              minimumSize: Size(0, 54.h),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12.r),
                              ),
                            ),
                            child: Text(
                              'CANCEL',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 15.sp,
                                fontWeight: FontWeight.w700,
                                letterSpacing: 1.5,
                                color: AppColors.foregroundMuted,
                              ),
                            ),
                          ),
                        ),
                        SizedBox(width: 10.w),
                        Expanded(
                          flex: 2,
                          child: Material(
                            color: Colors.transparent,
                            child: InkWell(
                              onTap: _confirm,
                              borderRadius: BorderRadius.circular(12.r),
                              child: Ink(
                                height: 54.h,
                                decoration: BoxDecoration(
                                  color: AppColors.primary,
                                  borderRadius: BorderRadius.circular(12.r),
                                  boxShadow: [
                                    BoxShadow(
                                      color: AppColors.primary
                                          .withValues(alpha: 0.30),
                                      blurRadius: 12,
                                      offset: const Offset(0, 4),
                                    ),
                                  ],
                                ),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.check_circle_outline_rounded,
                                        size: 18.r, color: Colors.white),
                                    SizedBox(width: 8.w),
                                    // Flexible where the full-width CREATE
                                    // ORDER button is not: this one only gets
                                    // two thirds of the row, so on a narrow
                                    // handset the label must shrink rather
                                    // than overflow.
                                    Flexible(
                                      child: Text(
                                        'CONFIRM & SAVE',
                                        maxLines: 1,
                                        overflow: TextOverflow.ellipsis,
                                        style: GoogleFonts.barlowCondensed(
                                          fontSize: 16.sp,
                                          fontWeight: FontWeight.w800,
                                          letterSpacing: 1.2,
                                          color: Colors.white,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
