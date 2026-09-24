import 'package:equatable/equatable.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/entities/proximity_policy.dart';
import 'package:uswatte/features/pricing/domain/entities/pricing_structure.dart';

enum LocationCheckStatus {
  checking,
  ready,
  serviceDisabled,
  permissionDenied,
  // GPS is on and permission is granted, but a fix couldn't be obtained in time
  // (weak signal / indoors / cold start) — distinct from serviceDisabled so the
  // UI doesn't send the rep to Settings when Settings is already fine.
  fixTimeout,
}

/// In-memory cart line during Create Bill editing.
///
/// `billingItemType` is the single source of truth for line kind:
///   - 'Sale'      → contributes to subtotal; charged to outlet
///   - 'FreeIssue' → contributes to freeIssueValue (informational); not charged
///   - 'Return'    → contributes to returnTotal; credit back to distributor
class CartLine extends Equatable {
  final int lineNumber;
  final ProductWithPrice product;
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType; // 'Sale' | 'FreeIssue' | 'Return'
  final String? returnType;     // 'Damage' | 'Expire' | 'MarketResell'
  final String? freeIssueSource; // 'Company' | 'Distributor' — only set when isFreeIssue
  final DateTime? expireDate;   // Only when returnType == 'Expire'
  final String priceType; // 'Case' | 'Packet'

  /// The pricing structure this line was priced from. Fixed when the line is
  /// added: switching the bill's structure later only affects new lines.
  final int? pricingStructureId;

  /// The structure's price for [priceType] when the line was added — the pack
  /// price for Packet, the full case price for Case. Kept on return lines too
  /// (so switching one back to a sale can restore the list price), but only
  /// sent to the server for structure-priced lines.
  final double? listUnitPrice;

  const CartLine({
    required this.lineNumber,
    required this.product,
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.billingItemType = 'Sale',
    this.returnType,
    this.freeIssueSource,
    this.expireDate,
    this.priceType = 'Packet',
    this.pricingStructureId,
    this.listUnitPrice,
  });

  bool get isFreeIssue => billingItemType == 'FreeIssue';
  bool get isReturn    => billingItemType == 'Return';
  bool get isSale      => billingItemType == 'Sale';

  /// Line total used for *display* and aggregation:
  /// - Sale:      qty × price × (1 − discount/100)
  /// - FreeIssue: qty × price  (informational FOC value; excluded from subtotal)
  /// - Return:    qty × price  (no discount applied to returns)
  double get lineTotal {
    if (isFreeIssue) return quantity * unitPrice;
    if (isReturn)    return quantity * unitPrice;
    final gross = quantity * unitPrice;
    final disc  = gross * discountRate / 100.0;
    return gross - disc;
  }

  /// The per-pack price [listUnitPrice] implies — what [unitPrice] is for a
  /// structure-priced line (quantities are always in packs). Null when unknown.
  double? get listPricePerPack {
    final list = listUnitPrice;
    if (list == null) return null;
    if (priceType != 'Case') return list;
    final packs = product.packsPerCase;
    return packs > 0 ? list / packs : list;
  }

  CartLine copyWith({
    int? lineNumber,
    double? quantity,
    double? unitPrice,
    double? discountRate,
    String? billingItemType,
    String? returnType,
    bool clearReturnType = false,
    String? freeIssueSource,
    bool clearFreeIssueSource = false,
    DateTime? expireDate,
    bool clearExpireDate = false,
    String? priceType,
  }) =>
      CartLine(
        lineNumber: lineNumber ?? this.lineNumber,
        product: product,
        quantity: quantity ?? this.quantity,
        unitPrice: unitPrice ?? this.unitPrice,
        discountRate: discountRate ?? this.discountRate,
        billingItemType: billingItemType ?? this.billingItemType,
        returnType: clearReturnType ? null : (returnType ?? this.returnType),
        freeIssueSource: clearFreeIssueSource
            ? null
            : (freeIssueSource ?? this.freeIssueSource),
        expireDate: clearExpireDate ? null : (expireDate ?? this.expireDate),
        priceType: priceType ?? this.priceType,
        pricingStructureId: pricingStructureId,
        listUnitPrice: listUnitPrice,
      );

  @override
  List<Object?> get props => [
        lineNumber,
        product.id,
        quantity,
        unitPrice,
        discountRate,
        billingItemType,
        returnType,
        freeIssueSource,
        expireDate,
        priceType,
        pricingStructureId,
        listUnitPrice,
      ];
}

class CreateBillState extends Equatable {
  final Outlet? outlet;

  /// Every locally synced (active) pricing structure, default first.
  final List<PricingStructure> pricingStructures;

  /// False until the local structures have been read once, so the page can
  /// tell "still loading" apart from "nothing synced".
  final bool pricingStructuresLoaded;

  /// The structure new lines are priced from. Changing it never touches lines
  /// already in the cart.
  final PricingStructure? selectedPricingStructure;
  final List<CartLine> cart;
  final double billDiscountRate;
  final bool submitting;
  final String? errorMessage;
  final String? submittedClientBillId;
  final double? latitude;
  final double? longitude;
  final LocationCheckStatus locationStatus;
  final bool refreshingLocation;

  /// Accuracy radius (m) the handset reported for the captured fix.
  final double? gpsAccuracyMeters;

  /// The rep's effective geofence policy, mirrored from OutletsBloc.
  final ProximityPolicy policy;

  const CreateBillState({
    this.outlet,
    this.pricingStructures = const [],
    this.pricingStructuresLoaded = false,
    this.selectedPricingStructure,
    this.cart = const [],
    this.billDiscountRate = 0,
    this.submitting = false,
    this.errorMessage,
    this.submittedClientBillId,
    this.latitude,
    this.longitude,
    this.locationStatus = LocationCheckStatus.checking,
    this.refreshingLocation = false,
    this.gpsAccuracyMeters,
    this.policy = const ProximityPolicy.enforcedAt(
        AppConstants.billingProximityRadiusMeters),
  });

  /// The radius the UI should describe to the rep. Kept as a getter so the many
  /// existing `state.radiusMeters` call sites keep working now that the policy,
  /// not a bare number, is the thing being carried.
  double get radiusMeters => policy.radiusMeters;

  /// Whether the distance filter applies right now. Ask this, never
  /// `policy.enforced` — a cached exemption has to expire on its own.
  bool get proximityEnforced => policy.isEnforcedNow;

  // Aggregates — each line type contributes to its own bucket only.
  double get saleSubTotal    => cart.where((l) => l.isSale     ).fold<double>(0, (s, l) => s + l.lineTotal);
  double get freeIssueValue  => cart.where((l) => l.isFreeIssue).fold<double>(0, (s, l) => s + l.lineTotal);
  double get freeIssueValueCompany =>
      cart.where((l) => l.isFreeIssue && l.freeIssueSource == 'Company')
          .fold<double>(0, (s, l) => s + l.lineTotal);
  double get freeIssueValueDistributor =>
      cart.where((l) => l.isFreeIssue && l.freeIssueSource == 'Distributor')
          .fold<double>(0, (s, l) => s + l.lineTotal);
  double get returnTotal     => cart.where((l) => l.isReturn   ).fold<double>(0, (s, l) => s + l.lineTotal);

  double get billDiscountAmount => saleSubTotal * billDiscountRate / 100.0;
  double get total              => saleSubTotal - billDiscountAmount - returnTotal;

  // Display-only breakdown (cart summary). saleSubTotal is already net of line
  // discounts, so show the gross sales and the discount as its own figure:
  // Sales (gross) − Discount (lines + bill) − Returns = total.
  double get saleGrossTotal =>
      cart.where((l) => l.isSale).fold<double>(0, (s, l) => s + l.quantity * l.unitPrice);
  double get lineDiscountTotal => saleGrossTotal - saleSubTotal;
  double get totalDiscountAmount => lineDiscountTotal + billDiscountAmount;
  double get totalDiscountPercent =>
      saleGrossTotal > 0 ? totalDiscountAmount / saleGrossTotal * 100.0 : 0;
  bool get hasDiscount => totalDiscountAmount > 0.004;

  bool get hasReturns    => cart.any((l) => l.isReturn);
  bool get hasFreeIssues => cart.any((l) => l.isFreeIssue);

  /// True when the cart holds lines priced from more than one structure — the
  /// cue for the cart to label each line with its structure.
  bool get cartMixesStructures =>
      cart.map((l) => l.pricingStructureId).toSet().length > 1;

  /// Display name of a structure id: its synced name, else "Price list #id".
  String? pricingStructureNameFor(int? id) {
    if (id == null) return null;
    for (final s in pricingStructures) {
      if (s.id == id) return s.name;
    }
    return 'Price list #$id';
  }

  bool get canSubmit =>
      locationStatus == LocationCheckStatus.ready &&
      outlet != null &&
      selectedPricingStructure != null &&
      cart.isNotEmpty &&
      cart.every((l) => !l.isReturn || l.returnType != null) &&
      cart.every((l) => !l.isFreeIssue || l.freeIssueSource != null) &&
      !submitting;

  CreateBillState copyWith({
    Outlet? outlet,
    List<PricingStructure>? pricingStructures,
    bool? pricingStructuresLoaded,
    PricingStructure? selectedPricingStructure,
    bool clearSelectedPricingStructure = false,
    List<CartLine>? cart,
    double? billDiscountRate,
    bool? submitting,
    String? errorMessage,
    String? submittedClientBillId,
    bool clearError = false,
    double? latitude,
    double? longitude,
    LocationCheckStatus? locationStatus,
    double? gpsAccuracyMeters,
    ProximityPolicy? policy,
    bool? refreshingLocation,
  }) =>
      CreateBillState(
        outlet: outlet ?? this.outlet,
        pricingStructures: pricingStructures ?? this.pricingStructures,
        pricingStructuresLoaded:
            pricingStructuresLoaded ?? this.pricingStructuresLoaded,
        selectedPricingStructure: clearSelectedPricingStructure
            ? null
            : (selectedPricingStructure ?? this.selectedPricingStructure),
        cart: cart ?? this.cart,
        billDiscountRate: billDiscountRate ?? this.billDiscountRate,
        submitting: submitting ?? this.submitting,
        errorMessage: clearError ? null : (errorMessage ?? this.errorMessage),
        submittedClientBillId:
            submittedClientBillId ?? this.submittedClientBillId,
        latitude: latitude ?? this.latitude,
        longitude: longitude ?? this.longitude,
        locationStatus: locationStatus ?? this.locationStatus,
        gpsAccuracyMeters: gpsAccuracyMeters ?? this.gpsAccuracyMeters,
        policy: policy ?? this.policy,
        refreshingLocation: refreshingLocation ?? this.refreshingLocation,
      );

  @override
  List<Object?> get props => [
        outlet?.id,
        pricingStructures,
        pricingStructuresLoaded,
        selectedPricingStructure,
        cart,
        billDiscountRate,
        submitting,
        errorMessage,
        submittedClientBillId,
        latitude,
        longitude,
        locationStatus,
        gpsAccuracyMeters,
        policy,
        refreshingLocation,
      ];
}
