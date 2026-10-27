CREATE TABLE IF NOT EXISTS virtual_card.re_preference_job_failure (
    payment_request_id BIGINT PRIMARY KEY
        REFERENCES virtual_card.payment_request(id),
    attempt_count      INTEGER NOT NULL DEFAULT 1,
    last_failed_on     TIMESTAMP(6) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_error         TEXT
);
