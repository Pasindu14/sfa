import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/di/injection.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/domain/usecases/search_products_for_bill_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_bloc.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/create_bill_state.dart';
import 'package:uswatte/features/bills/presentation/widgets/cart_list.dart';
import 'package:uswatte/features/bills/presentation/widgets/outlet_picker.dart';
import 'package:uswatte/features/bills/presentation/widgets/pricing_structure_picker.dart';
import 'package:uswatte/features/bills/presentation/widgets/product_search_field.dart';
import 'package:uswatte/features/bills/presentation/widgets/quantity_dialog.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_bloc.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_state.dart';

class CreateBillPage extends StatelessWidget {
  const CreateBillPage({super.key});

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocListener<CreateBillBloc, CreateBillState>(
      listenWhen: (prev, curr) =>
          prev.submittedClientBillId != curr.submittedClientBillId &&
          curr.submittedClientBillId != null,
      listener: (ctx, state) {
        ScaffoldMessenger.of(ctx).showSnackBar(
          SnackBar(
            content: Text(
              'Order saved — will sync when online',
              style: GoogleFonts.barlow(fontWeight: FontWeight.w500),
            ),
            backgroundColor: AppColors.darkSurface,
            duration: const Duration(seconds: 2),
          ),
        );
        ctx.goNamed('bills');
      },
      child: Scaffold(
        backgroundColor: AppColors.surface,
        body: Column(
          children: [
            // ── Orange gradient app bar ──────────────────────────────────
            _OrderAppBar(onBack: () => context.pop()),

            // ── Scrollable body ──────────────────────────────────────────
            Expanded(
              child: SingleChildScrollView(
                padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 16.h),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // ── Step 1: Outlet ───────────────────────────────────
                    _SectionLabel(
                      label: 'SELECT OUTLET',
                      icon: Icons.storefront_rounded,
                      step: '1',
                    ),
                    SizedBox(height: 10.h),
                    BlocBuilder<CreateBillBloc, CreateBillState>(
                      buildWhen: (p, c) => p.outlet != c.outlet,
                      builder: (ctx, state) =>
                          BlocBuilder<OutletsBloc, OutletsState>(
                        builder: (oCtx, oState) {
                          final outlets = oState is OutletsLoaded
                              ? oState.outlets
                              : const [];
                          return OutletPicker(
                            selected: state.outlet,
                            outlets: outlets.cast(),
                            onSelected: (o) =>
                                ctx.read<CreateBillBloc>().add(OutletSelected(o)),
                          );
                        },
                      ),
                    ),

                    SizedBox(height: 20.h),

                    // ── Step 2: Pricing Structure ────────────────────────
                    _SectionLabel(
                      label: 'PRICING STRUCTURE',
                      icon: Icons.price_change_rounded,
                      step: '2',
                    ),
                    SizedBox(height: 10.h),
                    BlocBuilder<CreateBillBloc, CreateBillState>(
                      buildWhen: (p, c) =>
                          p.selectedPricingStructure !=
                              c.selectedPricingStructure ||
                          p.pricingStructures != c.pricingStructures,
                      builder: (ctx, state) => PricingStructurePicker(
                        selected: state.selectedPricingStructure,
                        structures: state.pricingStructures,
                        onSelected: (s) => ctx
                            .read<CreateBillBloc>()
                            .add(PricingStructureSelected(s)),
                      ),
                    ),

                    SizedBox(height: 20.h),

                    // ── Step 3: Products ─────────────────────────────────
                    BlocBuilder<CreateBillBloc, CreateBillState>(
                      buildWhen: (p, c) =>
                          p.outlet != c.outlet ||
                          p.selectedPricingStructure !=
                              c.selectedPricingStructure,
                      builder: (ctx, state) {
                        final ready = state.outlet != null &&
                            state.selectedPricingStructure != null;
                        return Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            _SectionLabel(
                              label: 'ADD PRODUCTS',
                              icon: Icons.inventory_2_rounded,
                              step: '3',
                              dimmed: !ready,
                            ),
                            SizedBox(height: 10.h),
                            ProductSearchField(
                              enabled: ready,
                              searchUseCase:
                                  getIt<SearchProductsForBillUseCase>(),
                              pricingStructureId:
                                  state.selectedPricingStructure?.id,
                              onProductChosen: (product) async {
                                final qty = await showQuantityDialog(
                                  ctx,
                                  product: product,
                                );
                                if (qty != null && qty > 0) {
                                  if (!ctx.mounted) return;
                                  ctx
                                      .read<CreateBillBloc>()
                                      .add(ProductAdded(product, qty));
                                }
                              },
                            ),
                          ],
                        );
                      },
                    ),

                    SizedBox(height: 16.h),
                  ],
                ),
              ),
            ),

            // ── Sticky dark cart panel ───────────────────────────────────
            BlocBuilder<CreateBillBloc, CreateBillState>(
              builder: (_, state) => CartList(state: state),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Orange gradient header ────────────────────────────────────────────────────

class _OrderAppBar extends StatelessWidget {
  const _OrderAppBar({required this.onBack});
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
    return Container(
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
          padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 16.h),
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
                      'NEW ORDER',
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
                      'Outlet → Price list → Products',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: Colors.white.withValues(alpha: 0.70),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Section label with step number + orange left-bar ─────────────────────────

class _SectionLabel extends StatelessWidget {
  const _SectionLabel({
    required this.label,
    required this.icon,
    required this.step,
    this.dimmed = false,
  });

  final String label;
  final IconData icon;
  final String step;
  final bool dimmed;

  @override
  Widget build(BuildContext context) {
    final color = dimmed ? AppColors.foregroundMuted : AppColors.primary;
    return Row(
      children: [
        Container(
          width: 3.w,
          height: 13.h,
          decoration: BoxDecoration(
            color: dimmed
                ? AppColors.surfaceVariant
                : AppColors.primary,
            borderRadius: BorderRadius.circular(2.r),
          ),
        ),
        SizedBox(width: 8.w),
        Container(
          width: 16.r,
          height: 16.r,
          decoration: BoxDecoration(
            color: dimmed
                ? AppColors.surfaceVariant
                : AppColors.primary.withValues(alpha: 0.12),
            borderRadius: BorderRadius.circular(4.r),
          ),
          alignment: Alignment.center,
          child: Text(
            step,
            style: GoogleFonts.barlowCondensed(
              fontSize: 9.sp,
              fontWeight: FontWeight.w900,
              color: dimmed ? AppColors.foregroundMuted : AppColors.primary,
            ),
          ),
        ),
        SizedBox(width: 6.w),
        Icon(icon, size: 12.r, color: color),
        SizedBox(width: 4.w),
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 2.5,
            color: color,
          ),
        ),
      ],
    );
  }
}
