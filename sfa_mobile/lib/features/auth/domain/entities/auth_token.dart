class AuthToken {
  final String accessToken;
  final String? refreshToken;

  const AuthToken({required this.accessToken, this.refreshToken});
}
