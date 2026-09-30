import 'dart:async';

import 'package:dio/dio.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:flutter/foundation.dart';

class FcmService {
  final Dio _dio;

  FcmService(this._dio);

  StreamSubscription<String>? _tokenRefreshSub;

  /// Called after login and on session restore — requests permission, gets the
  /// FCM token, sends it to the API, and listens for token rotations.
  Future<void> registerToken() async {
    try {
      await FirebaseMessaging.instance.requestPermission(
        alert: true,
        badge: true,
        sound: true,
      );
      final token = await FirebaseMessaging.instance.getToken();
      if (token == null) return;
      await _sendToken(token);
      // Registration runs on every restore and every login, so replace the
      // rotation listener rather than stacking a new one each time.
      await _tokenRefreshSub?.cancel();
      _tokenRefreshSub =
          FirebaseMessaging.instance.onTokenRefresh.listen(_sendToken);
    } catch (e) {
      debugPrint('[FCM] registerToken failed: $e');
    }
  }

  /// Called before logout — clears the token from the API so no stale
  /// notifications are sent after the user signs out.
  Future<void> clearToken() async {
    await _tokenRefreshSub?.cancel();
    _tokenRefreshSub = null;
    try {
      await _dio.delete('/api/v1/users/me/fcm-token');
    } catch (e) {
      debugPrint('[FCM] clearToken failed: $e');
    }
  }

  Future<void> _sendToken(String token) async {
    try {
      await _dio.patch(
        '/api/v1/users/me/fcm-token',
        data: {'fcmToken': token},
      );
      debugPrint('[FCM] token registered');
    } catch (e) {
      debugPrint('[FCM] sendToken failed: $e');
    }
  }
}
