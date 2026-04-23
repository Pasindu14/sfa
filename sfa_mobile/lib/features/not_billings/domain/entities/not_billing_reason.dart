enum NotBillingReason {
  outletClosed,
  ownerAbsent,
  creditIssue,
  noOrder,
  outOfStock;

  String get apiValue => switch (this) {
        NotBillingReason.outletClosed => 'OutletClosed',
        NotBillingReason.ownerAbsent  => 'OwnerAbsent',
        NotBillingReason.creditIssue  => 'CreditIssue',
        NotBillingReason.noOrder      => 'NoOrder',
        NotBillingReason.outOfStock   => 'OutOfStock',
      };

  String get displayLabel => switch (this) {
        NotBillingReason.outletClosed => 'Outlet Closed',
        NotBillingReason.ownerAbsent  => 'Owner Absent',
        NotBillingReason.creditIssue  => 'Credit Issue',
        NotBillingReason.noOrder      => 'No Order',
        NotBillingReason.outOfStock   => 'Out of Stock',
      };

  static NotBillingReason fromApi(String value) => switch (value) {
        'OutletClosed' => NotBillingReason.outletClosed,
        'OwnerAbsent'  => NotBillingReason.ownerAbsent,
        'CreditIssue'  => NotBillingReason.creditIssue,
        'NoOrder'      => NotBillingReason.noOrder,
        'OutOfStock'   => NotBillingReason.outOfStock,
        _              => NotBillingReason.noOrder,
      };
}
