CREATE TABLE @o_tableName (
	`id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`ip_cidr` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
	`ip_dec` int(10) UNSIGNED NULL DEFAULT NULL,
	`address_count` int(10) UNSIGNED NOT NULL,
	`country_code` varchar(2) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT 'XX',
	`country_name` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
	`update_date` datetime(0) NULL DEFAULT NULL,
	`status` varchar(20) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
	`city` varchar(255) CHARACTER SET utf8 COLLATE utf8_general_ci NULL DEFAULT NULL,
	PRIMARY KEY (`id`) USING BTREE,
	INDEX `ip_dec`(`ip_dec`) USING BTREE
) ENGINE = InnoDB AUTO_INCREMENT = 1 CHARACTER SET = utf8 COLLATE = utf8_general_ci ROW_FORMAT = Compact;