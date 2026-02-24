CREATE OR REPLACE FUNCTION validate_session(p_token VARCHAR)
RETURNS TABLE(user_id INTEGER, is_valid BOOLEAN)
LANGUAGE SQL
STABLE
AS $$
    SELECT 
        s.user_id,
        (s.expires_at > CURRENT_TIMESTAMP) as is_valid
    FROM auth.sessions s
    WHERE s.token = p_token;
$$;
