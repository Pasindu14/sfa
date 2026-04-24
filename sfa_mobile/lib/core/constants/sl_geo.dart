class SLProvince {
  final int code;
  final String name;
  const SLProvince(this.code, this.name);
}

class SLDistrict {
  final int code;
  final String name;
  final int provinceCode;
  const SLDistrict(this.code, this.name, this.provinceCode);
}

const kProvinces = [
  SLProvince(1, 'Western'),
  SLProvince(2, 'Central'),
  SLProvince(3, 'Southern'),
  SLProvince(4, 'Northern'),
  SLProvince(5, 'Eastern'),
  SLProvince(6, 'North Western'),
  SLProvince(7, 'North Central'),
  SLProvince(8, 'Uva'),
  SLProvince(9, 'Sabaragamuwa'),
];

const kDistricts = [
  // Western
  SLDistrict(1, 'Colombo', 1),
  SLDistrict(2, 'Gampaha', 1),
  SLDistrict(3, 'Kalutara', 1),
  // Central
  SLDistrict(4, 'Kandy', 2),
  SLDistrict(5, 'Matale', 2),
  SLDistrict(6, 'Nuwara Eliya', 2),
  // Southern
  SLDistrict(7, 'Galle', 3),
  SLDistrict(8, 'Matara', 3),
  SLDistrict(9, 'Hambantota', 3),
  // Northern
  SLDistrict(10, 'Jaffna', 4),
  SLDistrict(11, 'Kilinochchi', 4),
  SLDistrict(12, 'Mannar', 4),
  SLDistrict(13, 'Vavuniya', 4),
  SLDistrict(14, 'Mullaitivu', 4),
  // Eastern
  SLDistrict(15, 'Batticaloa', 5),
  SLDistrict(16, 'Ampara', 5),
  SLDistrict(17, 'Trincomalee', 5),
  // North Western
  SLDistrict(18, 'Kurunegala', 6),
  SLDistrict(19, 'Puttalam', 6),
  // North Central
  SLDistrict(20, 'Anuradhapura', 7),
  SLDistrict(21, 'Polonnaruwa', 7),
  // Uva
  SLDistrict(22, 'Badulla', 8),
  SLDistrict(23, 'Monaragala', 8),
  // Sabaragamuwa
  SLDistrict(24, 'Ratnapura', 9),
  SLDistrict(25, 'Kegalle', 9),
];
