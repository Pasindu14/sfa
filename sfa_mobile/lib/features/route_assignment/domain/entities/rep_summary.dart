import 'package:equatable/equatable.dart';

class RepSummary extends Equatable {
  final int userId;
  final String userName;

  const RepSummary({required this.userId, required this.userName});

  @override
  List<Object> get props => [userId, userName];
}
