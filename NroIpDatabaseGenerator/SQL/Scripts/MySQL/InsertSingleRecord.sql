INSERT INTO 
	@o_tableName
	(`ip_cidr`, `ip_dec`, `address_count`, `country_code`, `country_name`, `update_date`, `status`, `city`)
VALUE (
	@ipcidr, @ipdec, @addressCount, @countryCode, @countryName, @updateDate, @_status, @city
);