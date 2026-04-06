import 'package:flutter/material.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/theme/app_theme.dart';

class DashboardPage extends StatelessWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(AppConstants.appName),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            onPressed: () {
              // TODO: dispatch LogoutRequested event
            },
          ),
        ],
      ),
      body: const Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.dashboard, size: 64, color: AppTheme.primary),
            SizedBox(height: 16),
            Text(
              'Dashboard',
              style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 8),
            Text('Features coming soon...', style: TextStyle(color: Colors.grey)),
          ],
        ),
      ),
    );
  }
}
