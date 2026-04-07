import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';

class RepSummaryModel extends RepSummary {
  const RepSummaryModel({required super.userId, required super.userName});

  factory RepSummaryModel.fromJson(Map<String, dynamic> json) =>
      RepSummaryModel(
        userId: json['userId'] as int,
        userName: json['userName'] as String,
      );
}
