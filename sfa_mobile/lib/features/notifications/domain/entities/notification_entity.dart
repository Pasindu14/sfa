class NotificationEntity {
  final int id;
  final String title;
  final String body;
  final String? data;
  final bool isRead;
  final DateTime createdAt;

  const NotificationEntity({
    required this.id,
    required this.title,
    required this.body,
    this.data,
    required this.isRead,
    required this.createdAt,
  });
}
