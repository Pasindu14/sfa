import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:geolocator/geolocator.dart';
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
import 'package:uswatte/features/bills/presentation/widgets/product_search_delegate.dart';
import 'package:uswatte/core/connectivity/connectivity_service.dart';
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
          (prev.submittedClientBillId != curr.submittedClientBillId &&
              curr.submittedClientBillId != null) ||
          (prev.errorMessage != curr.errorMessage &&
              curr.errorMessage != null),
      listener: (ctx, state) {
        if (state.submittedClientBillId != null &&
            state.errorMessage == null) {
          ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
            content: Text(
              'Order saved — will sync when online',
              style: GoogleFonts.barlow(
                  color: Colors.white, fontWeight: FontWeight.w500),
            ),
            backgroundColor: AppColors.success,
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.all(16.w),
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8.r)),
            duration: const Duration(seconds: 2),
          ));
          // Pop back to the existing bills list instead of goNamed('bills').
          // goNamed rebuilds the stack as [/sales-rep shell, bills] and discards the
          // SalesRepHome page underneath — so backing out of the list later lands on the
          // empty /sales-rep shell (a black screen). Popping preserves [home, bills, ...].
          // The list reloads itself on return (see BillsListPage's New Order handler).
          ctx.pop();
        } else if (state.errorMessage != null) {
          ScaffoldMessenger.of(ctx).showSnackBar(SnackBar(
            content: Text(
              state.errorMessage!,
              style: GoogleFonts.barlow(
                  color: Colors.white, fontWeight: FontWeight.w500),
            ),
            backgroundColor: AppColors.error,
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.all(16.w),
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8.r)),
            duration: const Duration(seconds: 4),
          ));
        }
      },
      child: Scaffold(
        backgroundColor: AppColors.surface,
        body: Column(
          children: [
            // ── Orange gradient app bar ──────────────────────────────────
            _OrderAppBar(onBack: () => context.pop()),

            // ── Body — gated by location status ─────────────────────────
            Expanded(
              child: BlocBuilder<CreateBillBloc, CreateBillState>(
                buildWhen: (p, c) => p.locationStatus != c.locationStatus,
                builder: (ctx, state) {
                  if (state.locationStatus == LocationCheckStatus.checking) {
                    return const Center(child: CircularProgressIndicator());
                  }
                  if (state.locationStatus != LocationCheckStatus.ready) {
                    return _LocationBlockedView(status: state.locationStatus);
                  }
                  // ── Location is ready — show full form ───────────────
                  return Column(
                    children: [
                      Expanded(
                        child: SingleChildScrollView(
                          padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 16.h),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.stretch,
                            children: [
                              // ── Step 1: Outlet ───────────────────────
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
                                    final hasAssignment =
                                        oState is OutletsLoaded
                                            ? oState.hasActiveAssignment
                                            : true;
                                    return Column(
                                      crossAxisAlignment:
                                          CrossAxisAlignment.stretch,
                                      children: [
                                        OutletPicker(
                                          selected: state.outlet,
                                          outlets: outlets.cast(),
                                          onSelected: (o) => ctx
                                              .read<CreateBillBloc>()
                                              .add(OutletSelected(o)),
                                          hasActiveAssignment: hasAssignment,
                                        ),
                                        if (state.outlet != null) ...[
                                          SizedBox(height: 8.h),
                                          _HistoryButton(
                                            outletId: state.outlet!.id,
                                            outletName: state.outlet!.name,
                                          ),
                                        ],
                                      ],
                                    );
                                  },
                                ),
                              ),

                              SizedBox(height: 20.h),

                              // ── Step 2: Pricing Structure ────────────
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
                                builder: (ctx, state) =>
                                    PricingStructurePicker(
                                  selected: state.selectedPricingStructure,
                                  structures: state.pricingStructures,
                                  onSelected: (s) => ctx
                                      .read<CreateBillBloc>()
                                      .add(PricingStructureSelected(s)),
                                ),
                              ),

                              SizedBox(height: 20.h),

                              // ── Step 3: Products ─────────────────────
                              BlocBuilder<CreateBillBloc, CreateBillState>(
                                buildWhen: (p, c) =>
                                    p.outlet != c.outlet ||
                                    p.selectedPricingStructure !=
                                        c.selectedPricingStructure ||
                                    p.cart.length != c.cart.length,
                                builder: (ctx, state) {
                                  final ready = state.outlet != null &&
                                      state.selectedPricingStructure != null;
                                  return Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.stretch,
                                    children: [
                                      _SectionLabel(
                                        label: 'ADD PRODUCTS',
                                        icon: Icons.inventory_2_rounded,
                                        step: '3',
                                        dimmed: !ready,
                                      ),
                                      SizedBox(height: 10.h),
                                      _AddProductsButton(
                                        enabled: ready,
                                        cartCount: state.cart.length,
                                        onTap: () => showProductSearch(
                                          ctx,
                                          searchUseCase:
                                              getIt<SearchProductsForBillUseCase>(),
                                          pricingStructureId:
                                              state.selectedPricingStructure
                                                  ?.id,
                                          onProductAdded: (product, qty,
                                                  unitPrice,
                                                  discountRate,
                                                  billingItemType,
                                                  returnType,
                                                  freeIssueSource,
                                                  expireDate) =>
                                              ctx.read<CreateBillBloc>().add(
                                                    ProductAdded(
                                                      product,
                                                      qty,
                                                      unitPrice: unitPrice,
                                                      discountRate:
                                                          discountRate,
                                                      billingItemType:
                                                          billingItemType,
                                                      returnType: returnType,
                                                      freeIssueSource:
                                                          freeIssueSource,
                                                      expireDate: expireDate,
                                                    ),
                                                  ),
                                        ),
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

                      // ── Sticky dark cart panel ───────────────────────
                      BlocBuilder<CreateBillBloc, CreateBillState>(
                        builder: (_, state) => CartList(state: state),
                      ),
                    ],
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Location blocked view ─────────────────────────────────────────────────────

class _LocationBlockedView extends StatelessWidget {
  const _LocationBlockedView({required this.status});
  final LocationCheckStatus status;

  @override
  Widget build(BuildContext context) {
    final isServiceOff = status == LocationCheckStatus.serviceDisabled;
    return Padding(
      padding: EdgeInsets.all(24.r),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 72.r,
            height: 72.r,
            decoration: BoxDecoration(
              color: AppColors.error.withValues(alpha: 0.10),
              shape: BoxShape.circle,
            ),
            child: Icon(
              Icons.location_off_rounded,
              size: 36.r,
              color: AppColors.error,
            ),
          ),
          SizedBox(height: 20.h),
          Text(
            'Location Required',
            style: GoogleFonts.barlowCondensed(
              fontSize: 22.sp,
              fontWeight: FontWeight.w800,
              letterSpacing: 0.5,
              color: AppColors.foreground,
            ),
          ),
          SizedBox(height: 8.h),
          Text(
            isServiceOff
                ? 'GPS is turned off on your device. Please enable Location Services to create a bill.'
                : 'Location permission was denied. Please allow location access for this app to continue.',
            textAlign: TextAlign.center,
            style: GoogleFonts.barlow(
              fontSize: 14.sp,
              color: AppColors.foregroundMuted,
              height: 1.5,
            ),
          ),
          SizedBox(height: 28.h),
          SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: () async {
                if (isServiceOff) {
                  await Geolocator.openLocationSettings();
                } else {
                  await Geolocator.openAppSettings();
                }
              },
              icon: Icon(Icons.settings_rounded, size: 18.r),
              label: Text(
                isServiceOff ? 'Open Location Settings' : 'Open App Settings',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 15.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.5,
                ),
              ),
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                foregroundColor: Colors.white,
                padding: EdgeInsets.symmetric(vertical: 14.h),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
              ),
            ),
          ),
          SizedBox(height: 12.h),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: () =>
                  context.read<CreateBillBloc>().add(const LocationCheckRetried()),
              icon: Icon(Icons.refresh_rounded, size: 18.r),
              label: Text(
                'Retry',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 15.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.5,
                ),
              ),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: BorderSide(color: AppColors.primary.withValues(alpha: 0.40)),
                padding: EdgeInsets.symmetric(vertical: 14.h),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── History button (shown when outlet selected) ───────────────────────────────

class _HistoryButton extends StatelessWidget {
  final int outletId;
  final String outletName;

  const _HistoryButton({
    required this.outletId,
    required this.outletName,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () async {
        final online = await getIt<ConnectivityService>().hasInternet();
        if (!context.mounted) return;
        if (!online) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text(
                'No internet connection — history unavailable',
                style: GoogleFonts.barlow(fontWeight: FontWeight.w500),
              ),
              backgroundColor: AppColors.darkSurface,
              duration: const Duration(seconds: 2),
            ),
          );
          return;
        }
        context.pushNamed(
          'outletBillHistory',
          extra: {'outletId': outletId, 'outletName': outletName},
        );
      },
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 10.h),
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.06),
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(color: AppColors.primary.withValues(alpha: 0.20)),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.history_rounded, size: 15.r, color: AppColors.primary),
            SizedBox(width: 6.w),
            Text(
              'View Outlet Bill History',
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 0.5,
                color: AppColors.primary,
              ),
            ),
            const Spacer(),
            Icon(Icons.chevron_right_rounded,
                size: 14.r, color: AppColors.primary),
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

// ── Add Products button ───────────────────────────────────────────────────────

class _AddProductsButton extends StatelessWidget {
  const _AddProductsButton({
    required this.enabled,
    required this.cartCount,
    required this.onTap,
  });

  final bool enabled;
  final int cartCount;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        padding: EdgeInsets.all(16.r),
        decoration: BoxDecoration(
          color: enabled ? Colors.white : AppColors.surface,
          borderRadius: BorderRadius.circular(14.r),
          border: Border.all(
            color: enabled
                ? AppColors.primary.withValues(alpha: 0.30)
                : AppColors.surfaceVariant,
          ),
          boxShadow: enabled
              ? [
                  BoxShadow(
                    color: AppColors.foreground.withValues(alpha: 0.04),
                    blurRadius: 10,
                    offset: const Offset(0, 3),
                  ),
                ]
              : null,
        ),
        child: Row(
          children: [
            Container(
              width: 38.r,
              height: 38.r,
              decoration: BoxDecoration(
                color: enabled
                    ? AppColors.primary.withValues(alpha: 0.10)
                    : AppColors.surfaceVariant,
                borderRadius: BorderRadius.circular(10.r),
              ),
              child: Icon(
                Icons.search_rounded,
                size: 18.r,
                color: enabled
                    ? AppColors.primary
                    : AppColors.foregroundMuted,
              ),
            ),
            SizedBox(width: 12.w),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    enabled ? 'SEARCH PRODUCTS' : 'COMPLETE STEPS 1 & 2 FIRST',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 9.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 2.0,
                      color: enabled
                          ? AppColors.primary
                          : AppColors.foregroundMuted,
                    ),
                  ),
                  SizedBox(height: 2.h),
                  Text(
                    cartCount > 0
                        ? 'Tap to add more products'
                        : 'Tap to open product search',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 14.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.3,
                      color: enabled
                          ? AppColors.foreground
                          : AppColors.foregroundMuted,
                    ),
                  ),
                ],
              ),
            ),
            if (cartCount > 0) ...[
              Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.10),
                  borderRadius: BorderRadius.circular(8.r),
                ),
                child: Text(
                  '$cartCount added',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w800,
                    color: AppColors.primary,
                  ),
                ),
              ),
              SizedBox(width: 8.w),
            ],
            Icon(
              Icons.chevron_right_rounded,
              size: 18.r,
              color: enabled
                  ? AppColors.foregroundMuted
                  : AppColors.surfaceVariant,
            ),
          ],
        ),
      ),
    );
  }
}
