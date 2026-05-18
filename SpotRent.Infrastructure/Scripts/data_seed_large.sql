-- Large volume seed to be executed AFTER SpotRent.Infrastructure/data_seed.sql
-- Generates:
--   - 15,000 addresses
--   - 15,000 spaces (all columns populated as in main seed)
--   - 105,000 working-hours rows (7 days per space)
--   - 100,000 bookings (all columns populated as in main seed)

BEGIN;

-- 1) Create 15,000 new addresses
CREATE TEMP TABLE tmp_new_addresses AS
WITH inserted AS (
    INSERT INTO address (id, building, street, city, region)
    SELECT
        99 + gs,
        (100 + gs)::text,
        'Load Street ' || gs,
        'Kharkiv',
        'Kharkiv Oblast'
    FROM generate_series(1, 15000) AS gs
    RETURNING id
)
SELECT row_number() OVER (ORDER BY id) AS rn, id
FROM inserted;

-- 2) Create 15,000 spaces using all columns populated in main seed
CREATE TEMP TABLE tmp_new_spaces AS
WITH inserted AS (
    INSERT INTO spaces (
        id,
        name,
        description,
        space_type,
        capacity,
        hourly_rate,
        image_url,
        address_id,
        area_sqm,
        owner_id,
        room,
        created_at
    )
    SELECT
        99 + a.rn,
        'Load Test Space #' || a.rn,
        'Auto-generated load-test space #' || a.rn,
        (a.rn % 5),
        1 + (a.rn % 60),
        (8 + (a.rn % 70))::numeric(10,2),
        'https://images.unsplash.com/photo-1497366216548-37526070297c',
        a.id,
        (6 + (a.rn % 180))::numeric(10,2),
        CASE WHEN a.rn % 2 = 0 THEN 2 ELSE 3 END,
        'Room ' || (100 + (a.rn % 900)),
        CURRENT_TIMESTAMP
    FROM tmp_new_addresses AS a
    ORDER BY a.rn
    RETURNING id, hourly_rate
)
SELECT row_number() OVER (ORDER BY id) AS rn, id, hourly_rate
FROM inserted;

-- 3) Add working hours for all created spaces (7 rows per space)
INSERT INTO working_hours (space_id, day_of_week, open_time, close_time, is_closed)
SELECT
    s.id,
    d.day_of_week,
    d.open_time,
    d.close_time,
    d.is_closed
FROM tmp_new_spaces AS s
CROSS JOIN (
    VALUES
        (1, '08:00'::time, '20:00'::time, false),
        (2, '08:00'::time, '20:00'::time, false),
        (3, '08:00'::time, '20:00'::time, false),
        (4, '08:00'::time, '20:00'::time, false),
        (5, '08:00'::time, '20:00'::time, false),
        (6, '10:00'::time, '18:00'::time, false),
        (0, '00:00'::time, '00:00'::time, true)
) AS d(day_of_week, open_time, close_time, is_closed);

-- 4) Create 100,000 bookings using all columns populated in main seed
INSERT INTO bookings (
    id,
    user_id,
    space_id,
    start_time,
    end_time,
    status,
    total_amount,
    payment_status,
    created_at,
    updated_at,
    cancelled_at
)
SELECT
    99 + g.gs AS id,
    4 + (g.gs % 5) AS user_id,
    s.id AS space_id,
    t.start_time,
    t.start_time + make_interval(hours => t.duration_hours) AS end_time,
    t.status,
    round((s.hourly_rate * t.duration_hours)::numeric, 2) AS total_amount,
    t.payment_status,
    t.created_at,
    t.updated_at,
    CASE
        WHEN t.status = 3 THEN t.updated_at
        ELSE NULL
    END AS cancelled_at
FROM generate_series(1, 100000) AS g(gs)
JOIN tmp_new_spaces AS s
    ON s.rn = ((g.gs - 1) % 15000) + 1
CROSS JOIN LATERAL (
    SELECT
        (CURRENT_TIMESTAMP + (((g.gs % 90) - 30) || ' days')::interval + ((g.gs % 16) || ' hours')::interval) AS start_time,
        1 + (g.gs % 6) AS duration_hours,
        CASE
            WHEN g.gs % 20 = 0 THEN 3
            WHEN g.gs % 3 = 0 THEN 2
            ELSE 0
        END AS status,
        CASE
            WHEN g.gs % 20 = 0 THEN 3
            WHEN g.gs % 4 = 0 THEN 0
            ELSE 1
        END AS payment_status,
        (CURRENT_TIMESTAMP - ((g.gs % 45) || ' days')::interval) AS created_at,
        (CURRENT_TIMESTAMP - ((g.gs % 44) || ' days')::interval) AS updated_at
) AS t;

COMMIT;
