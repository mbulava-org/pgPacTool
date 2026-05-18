CREATE OR REPLACE VIEW public.user_order_summary AS
SELECT
    u.id AS user_id,
    u.name AS user_name,
    u.email,
    COUNT(o.id) AS total_orders,
    SUM(o.total_amount) AS total_spent,
    MAX(o.created_at) AS last_order_date
FROM public.users u
LEFT JOIN public.orders o ON u.id = o.user_id
GROUP BY u.id, u.name, u.email;
