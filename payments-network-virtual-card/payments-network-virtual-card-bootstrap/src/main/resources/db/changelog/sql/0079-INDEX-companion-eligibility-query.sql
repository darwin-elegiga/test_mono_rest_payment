-- Indexes for companion eligibility query in PaymentRequestRepository.findEligibleCompanionCorrelationIds

CREATE INDEX IF NOT EXISTS idx_batch_detail_status_correlation_id
    ON virtual_card.batch_detail (status, correlation_id);

CREATE INDEX IF NOT EXISTS idx_payment_request_correlation_status_payment_details
    ON virtual_card.payment_request (correlation_id, status, payment_details_id);


