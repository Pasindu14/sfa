enum UserRole { admin, nsm, rsm, asm, supervisor, salesRep, distributor }

UserRole userRoleFromString(String raw) => switch (raw.toLowerCase()) {
      'admin' => UserRole.admin,
      'nsm' => UserRole.nsm,
      'rsm' => UserRole.rsm,
      'asm' => UserRole.asm,
      'supervisor' => UserRole.supervisor,
      'salesrep' => UserRole.salesRep,
      'distributor' => UserRole.distributor,
      _ => UserRole.salesRep,
    };
