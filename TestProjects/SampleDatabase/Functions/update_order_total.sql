CREATE OR REPLACE FUNCTION public.update_order_total()
RETURNS trigger AS $$
BEGIN
    UPDATE public.orders
    SET total_amount = public.calculate_order_total(NEW.order_id),
        updated_at = NOW()
    WHERE id = NEW.order_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
