CREATE INDEX IF NOT EXISTS ix_addresses_city
    ON address (city);

CREATE INDEX IF NOT EXISTS ix_spaces_hourly_rate
    ON spaces (hourly_rate);

CREATE INDEX IF NOT EXISTS ix_spaces_created_at_desc
    ON spaces (created_at DESC);

CREATE INDEX IF NOT EXISTS ix_attribute_values_attribute_id_value_space_id
    ON attribute_value (attribute_id, value, space_id);

CREATE INDEX IF NOT EXISTS ix_subscription_plans_is_active
    ON subscription_plans (is_active);
