class LoginRequestModel {
  final String username;
  final String password;
  final String deviceId;

  const LoginRequestModel({
    required this.username,
    required this.password,
    required this.deviceId,
  });

  Map<String, dynamic> toJson() => {
        'username': username,
        'password': password,
        'deviceId': deviceId,
      };
}
