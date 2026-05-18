CREATE TRIGGER trg_order_items_update_total
AFTER INSERT OR UPDATE OR DELETE ON public.order_items
FOR EACH ROW EXECUTE FUNCTION public.update_order_total();
