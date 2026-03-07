-- Insert Roles
INSERT INTO asp_net_roles (id, name, normalized_name, concurrency_stamp) VALUES
(2, 'Admin', 'ADMIN', 'admin-stamp-001'),
(3, 'SpaceOwner', 'SPACEOWNER', 'owner-stamp-002'),
(1, 'User', 'USER', 'user-stamp-003'),
(4, 'Manager', 'MANAGER', 'manager-stamp-004');

-- Insert Subscription Plans
INSERT INTO subscription_plans (id, name, description, price, duration, included_hours, is_active, owner_id, created_at, updated_at) VALUES
(1, 'Basic Plan', 'Perfect for occasional users', 29.99, 30, 10, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(2, 'Standard Plan', 'Great for regular coworkers', 79.99, 30, 40, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(3, 'Premium Plan', 'Unlimited access for professionals', 149.99, 30, 100, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(4, 'Enterprise Plan', 'Custom solution for teams', 499.99, 30, 500, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(5, 'Student Plan', 'Special pricing for students', 19.99, 30, 8, true, 3, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert Users (password hash is for "Password123!")
INSERT INTO asp_net_users (id, first_name, last_name, role, user_name, normalized_user_name, email, normalized_email,
    email_confirmed, password_hash, security_stamp, concurrency_stamp, phone_number, phone_number_confirmed,
    two_factor_enabled, lockout_enabled, access_failed_count, google_id, picture_url, created_at, updated_at) VALUES
(1, 'John', 'Admin', 0, 'john.admin', 'JOHN.ADMIN', 'john.admin@example.com', 'JOHN.ADMIN@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY001', 'CONCURRENCY001',
    '+380501234567', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=12', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(2, 'Sarah', 'Johnson', 1, 'sarah.johnson', 'SARAH.JOHNSON', 'sarah.johnson@example.com', 'SARAH.JOHNSON@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY002', 'CONCURRENCY002',
    '+380502345678', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=45', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(3, 'Michael', 'Smith', 1, 'michael.smith', 'MICHAEL.SMITH', 'michael.smith@example.com', 'MICHAEL.SMITH@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY003', 'CONCURRENCY003',
    '+380503456789', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=33', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(4, 'Emily', 'Davis', 2, 'emily.davis', 'EMILY.DAVIS', 'emily.davis@example.com', 'EMILY.DAVIS@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY004', 'CONCURRENCY004',
    '+380504567890', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=22', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(5, 'David', 'Wilson', 2, 'david.wilson', 'DAVID.WILSON', 'david.wilson@example.com', 'DAVID.WILSON@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY005', 'CONCURRENCY005',
    '+380505678901', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=51', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(6, 'Lisa', 'Martinez', 2, 'lisa.martinez', 'LISA.MARTINEZ', 'lisa.martinez@example.com', 'LISA.MARTINEZ@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY006', 'CONCURRENCY006',
    '+380506789012', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=29', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(7, 'James', 'Brown', 2, 'james.brown', 'JAMES.BROWN', 'james.brown@example.com', 'JAMES.BROWN@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY007', 'CONCURRENCY007',
    '+380507890123', true, false, true, 0, '', 'https://i.pravatar.cc/150?img=68', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(8, 'Anna', 'Taylor', 2, 'anna.taylor', 'ANNA.TAYLOR', 'anna.taylor@example.com', 'ANNA.TAYLOR@EXAMPLE.COM',
    true, 'AQAAAAEAACcQAAAAEJ3xqxKzx5L9fKPq6xqxKzx5L9fKPq', 'SECURITY008', 'CONCURRENCY008',
    '+380508901234', true, false, true, 0, 'google123', 'https://i.pravatar.cc/150?img=16', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert User Roles
INSERT INTO asp_net_user_roles (user_id, role_id) VALUES
(1, 2), -- John is Admin
(2, 3), -- Sarah is SpaceOwner
(3, 3), -- Michael is SpaceOwner
(4, 1), -- Emily is User
(5, 1), -- David is User
(6, 1), -- Lisa is User
(7, 1), -- James is User
(8, 1); -- Anna is User

-- Insert Addresses
INSERT INTO address (id, building, street, city, region) VALUES
(1, '15A', 'Sumska Street', 'Kharkiv', 'Kharkiv Oblast'),
(2, '42', 'Pushkinska Street', 'Kharkiv', 'Kharkiv Oblast'),
(3, '7B', 'Nauky Avenue', 'Kharkiv', 'Kharkiv Oblast'),
(4, '23', 'Heroiv Pratsi Street', 'Kharkiv', 'Kharkiv Oblast'),
(5, '101', 'Klochkivska Street', 'Kharkiv', 'Kharkiv Oblast'),
(6, '88', 'Moskovsky Avenue', 'Kharkiv', 'Kharkiv Oblast'),
(7, '5', 'Kulykivska Street', 'Kharkiv', 'Kharkiv Oblast');

-- Insert Spaces
INSERT INTO spaces (id, name, description, space_type, capacity, hourly_rate, image_url, address_id, area_sqm, owner_id, room, created_at) VALUES
(1, 'Creative Hub Meeting Room', 'Modern meeting room with whiteboard and projector', 0, 12, 25.00, 'https://images.unsplash.com/photo-1497366216548-37526070297c', 1, 35.5, 2, 'Room 301', CURRENT_TIMESTAMP),
(2, 'Downtown Conference Hall', 'Large conference space for corporate events', 1, 50, 75.00, 'https://images.unsplash.com/photo-1497366811353-6870744d04b2', 2, 120.0, 2, 'Hall A', CURRENT_TIMESTAMP),
(3, 'Startup Desk #12', 'Individual hot desk in open space', 2, 1, 8.50, 'https://images.unsplash.com/photo-1497366754035-f200968a6e72', 3, 2.5, 3, 'Open Space Floor 2', CURRENT_TIMESTAMP),
(4, 'Executive Private Office', 'Fully furnished private office with city view', 3, 4, 35.00, 'https://images.unsplash.com/photo-1497215728101-856f4ea42174', 4, 25.0, 3, 'Office 205', CURRENT_TIMESTAMP),
(5, 'Innovation Lab Workshop', 'Collaborative workshop space with tools and equipment', 4, 20, 45.00, 'https://images.unsplash.com/photo-1497215842964-222b430dc094', 5, 85.0, 2, 'Workshop 1', CURRENT_TIMESTAMP),
(6, 'Quiet Focus Room', 'Small private room for focused work', 0, 2, 15.00, 'https://images.unsplash.com/photo-1497366412874-3415097a27e7', 1, 8.0, 2, 'Room 105', CURRENT_TIMESTAMP),
(7, 'Team Collaboration Space', 'Open collaborative area with standing desks', 2, 8, 20.00, 'https://images.unsplash.com/photo-1497215728101-856f4ea42174', 6, 40.0, 3, 'Floor 3 West', CURRENT_TIMESTAMP),
(8, 'Presentation Theater', 'Theater-style room with AV equipment', 1, 30, 60.00, 'https://images.unsplash.com/photo-1497366811353-6870744d04b2', 7, 95.0, 2, 'Theater Room', CURRENT_TIMESTAMP);

-- Insert Attributes
INSERT INTO attribute (id, name, data_type, unit) VALUES
(1, 'WiFi Speed', 'integer', 'Mbps'),
(2, 'Power Outlets', 'integer', 'count'),
(3, 'Monitors', 'integer', 'count'),
(4, 'Whiteboard', 'boolean', null),
(5, 'Projector', 'boolean', null),
(6, 'Coffee Machine', 'boolean', null),
(7, 'Natural Light', 'boolean', null),
(8, 'Sound System', 'boolean', null);

-- Insert Attribute Values
INSERT INTO attribute_value (id, value, min_value, max_value, space_id, attribute_id) VALUES
(1, '500', 500, null, 1, 1),
(2, '8', 8, null, 1, 2),
(3, 'true', null, null, 1, 4),
(4, 'true', null, null, 1, 5),
(5, '1000', 1000, null, 2, 1),
(6, '20', 20, null, 2, 2),
(7, 'true', null, null, 2, 5),
(8, 'true', null, null, 2, 8),
(9, '300', 300, null, 3, 1),
(10, '2', 2, null, 3, 2),
(11, 'true', null, null, 4, 7),
(12, '4', 4, null, 4, 2),
(13, '2', 2, null, 4, 3),
(14, '500', 500, null, 5, 1),
(15, 'true', null, null, 5, 4),
(16, 'true', null, null, 5, 6);

-- Insert Working Hours (Monday = 1, Sunday = 0)
INSERT INTO working_hours (space_id, day_of_week, open_time, close_time, is_closed) VALUES
-- Space 1: Creative Hub Meeting Room
(1, 1, '08:00', '20:00', false),
(1, 2, '08:00', '20:00', false),
(1, 3, '08:00', '20:00', false),
(1, 4, '08:00', '20:00', false),
(1, 5, '08:00', '20:00', false),
(1, 6, '10:00', '18:00', false),
(1, 0, '00:00', '00:00', true),
-- Space 2: Downtown Conference Hall
(2, 1, '07:00', '22:00', false),
(2, 2, '07:00', '22:00', false),
(2, 3, '07:00', '22:00', false),
(2, 4, '07:00', '22:00', false),
(2, 5, '07:00', '22:00', false),
(2, 6, '09:00', '20:00', false),
(2, 0, '09:00', '20:00', false),
-- Space 3: Startup Desk
(3, 1, '00:00', '23:59', false),
(3, 2, '00:00', '23:59', false),
(3, 3, '00:00', '23:59', false),
(3, 4, '00:00', '23:59', false),
(3, 5, '00:00', '23:59', false),
(3, 6, '00:00', '23:59', false),
(3, 0, '00:00', '23:59', false);

-- Insert Devices
INSERT INTO devices (id, space_id, device_name, status, is_online, installed_at, updated_at) VALUES
(1, 1, 'Smart Lock CR301', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(2, 2, 'Access Control HallA', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(3, 3, 'Desk Sensor #12', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(4, 4, 'Smart Lock Off205', 1, false, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(5, 5, 'Workshop Controller', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(6, 6, 'Focus Room Lock', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP),
(7, 8, 'Theater Access Control', 1, true, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);

-- Insert Subscriptions
INSERT INTO subscriptions (id, user_id, subscription_plan_id, price, start_date, end_date, status, hours_used, created_at, updated_at) VALUES
(1, 4, 2, 79.99, CURRENT_TIMESTAMP - INTERVAL '15 days', CURRENT_TIMESTAMP + INTERVAL '15 days', 1, 15, CURRENT_TIMESTAMP - INTERVAL '15 days', CURRENT_TIMESTAMP),
(2, 5, 1, 29.99, CURRENT_TIMESTAMP - INTERVAL '20 days', CURRENT_TIMESTAMP + INTERVAL '10 days', 1, 5, CURRENT_TIMESTAMP - INTERVAL '20 days', CURRENT_TIMESTAMP),
(3, 6, 3, 149.99, CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP + INTERVAL '20 days', 1, 40, CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP),
(4, 7, 2, 79.99, CURRENT_TIMESTAMP - INTERVAL '25 days', CURRENT_TIMESTAMP + INTERVAL '5 days', 1, 35, CURRENT_TIMESTAMP - INTERVAL '25 days', CURRENT_TIMESTAMP),
(5, 8, 5, 19.99, CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP + INTERVAL '25 days', 1, 3, CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP);

-- Insert Bookings
INSERT INTO bookings (id, user_id, space_id, start_time, end_time, status, total_amount, payment_status, created_at, updated_at, cancelled_at) VALUES
(1, 4, 1, CURRENT_TIMESTAMP + INTERVAL '1 day', CURRENT_TIMESTAMP + INTERVAL '1 day 2 hours', 0, 50.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, null),
(2, 5, 3, CURRENT_TIMESTAMP + INTERVAL '2 days', CURRENT_TIMESTAMP + INTERVAL '2 days 4 hours', 0, 34.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, null),
(3, 6, 2, CURRENT_TIMESTAMP + INTERVAL '3 days', CURRENT_TIMESTAMP + INTERVAL '3 days 3 hours', 0, 225.00, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, null),
(4, 7, 4, CURRENT_TIMESTAMP - INTERVAL '2 days', CURRENT_TIMESTAMP - INTERVAL '2 days' + INTERVAL '5 hours', 2, 175.00, 1, CURRENT_TIMESTAMP - INTERVAL '3 days', CURRENT_TIMESTAMP - INTERVAL '2 days', null),
(5, 8, 6, CURRENT_TIMESTAMP - INTERVAL '5 days', CURRENT_TIMESTAMP - INTERVAL '5 days' + INTERVAL '3 hours', 2, 45.00, 1, CURRENT_TIMESTAMP - INTERVAL '6 days', CURRENT_TIMESTAMP - INTERVAL '5 days', null),
(6, 4, 5, CURRENT_TIMESTAMP - INTERVAL '10 days', CURRENT_TIMESTAMP - INTERVAL '10 days' + INTERVAL '4 hours', 3, 180.00, 3, CURRENT_TIMESTAMP - INTERVAL '12 days', CURRENT_TIMESTAMP - INTERVAL '11 days', CURRENT_TIMESTAMP - INTERVAL '11 days'),
(7, 5, 7, CURRENT_TIMESTAMP + INTERVAL '4 days', CURRENT_TIMESTAMP + INTERVAL '4 days 6 hours', 0, 120.00, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, null),
(8, 6, 8, CURRENT_TIMESTAMP + INTERVAL '5 days', CURRENT_TIMESTAMP + INTERVAL '5 days 2 hours', 0, 120.00, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP, null);

-- Insert Access Logs
INSERT INTO access_logs (id, user_id, space_id, device_id, access_type, timestamp, is_successful, error_message) VALUES
(1, 4, 1, 1, 0, CURRENT_TIMESTAMP - INTERVAL '2 hours', true, null),
(2, 5, 3, 3, 0, CURRENT_TIMESTAMP - INTERVAL '3 hours', true, null),
(3, 6, 2, 2, 0, CURRENT_TIMESTAMP - INTERVAL '1 hour', true, null),
(4, 7, 4, 4, 0, CURRENT_TIMESTAMP - INTERVAL '4 hours', false, 'Device offline'),
(5, 4, 1, 1, 1, CURRENT_TIMESTAMP - INTERVAL '30 minutes', true, null),
(6, 8, 6, 6, 0, CURRENT_TIMESTAMP - INTERVAL '5 hours', true, null),
(7, 5, 3, 3, 1, CURRENT_TIMESTAMP - INTERVAL '1 hour 30 minutes', true, null),
(8, 6, 2, 2, 1, CURRENT_TIMESTAMP - INTERVAL '45 minutes', true, null),
(9, 7, 5, 5, 0, CURRENT_TIMESTAMP - INTERVAL '6 hours', true, null),
(10, 4, 8, 7, 0, CURRENT_TIMESTAMP - INTERVAL '20 minutes', true, null);

-- Update sequences to continue from inserted IDs
SELECT setval('"AspNetRoles_Id_seq"', (SELECT MAX(id) FROM asp_net_roles));
SELECT setval('subscription_plan_subscription_plan_id_seq', (SELECT MAX(id) FROM subscription_plans));
SELECT setval('user_user_id_seq', (SELECT MAX(id) FROM asp_net_users));
SELECT setval('space_space_id_seq', (SELECT MAX(id) FROM spaces));
SELECT setval('device_device_id_seq', (SELECT MAX(id) FROM devices));
SELECT setval('subscription_subscription_id_seq', (SELECT MAX(id) FROM subscriptions));
SELECT setval('booking_booking_id_seq', (SELECT MAX(id) FROM bookings));
SELECT setval('access_log_access_log_id_seq', (SELECT MAX(id) FROM access_logs));
