CREATE SCHEMA IF NOT EXISTS virtual_card;
CREATE TABLE virtual_card.VIRTUAL_CARD_DOCUMENT_CUSTOM_REMARKS (
	id int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    org_id VARCHAR(36) NOT NULL,
    account_id VARCHAR(36) NOT NULL,
    sub_account_id VARCHAR(36) NOT NULL,
    logo_url TEXT NOT NULL,
    custom_remarks1 TEXT NOT NULL,
    custom_remarks2 TEXT NOT NULL,
    custom_remarks3 TEXT NOT NULL,
    custom_remarks4 TEXT NOT NULL,
    custom_remarks5 TEXT NOT NULL,
    custom_remarks6 TEXT NOT NULL
);

INSERT INTO virtual_card.VIRTUAL_CARD_DOCUMENT_CUSTOM_REMARKS
(org_id, account_id, sub_account_id, logo_url, custom_remarks1, custom_remarks2, custom_remarks3, custom_remarks4, custom_remarks5, custom_remarks6)
VALUES('123', '12121', '121212', '', 'Electronic Payment Remittance Advice', 'Advice of Payment - VCC',
'The document you are holding is a payment via virtual Credit Card for services provided. This document is sent on behalf of',
' who has partnered with optum to provide a faster, more efficient way to reimburse your business.',
'<strong>Please process this credit card immediately.</strong><br/><br/> This is a one-time use stored value card. It can not be overcharged and will not allow additional charges. All transactions are tracked and audited.<br/><br/><strong>Notice:</strong> Payment of any charges associated with this repair may be adjusted to match the amount authorized or if the amount listed on your invoice is less than the originally authorized amount.<br/><br/><strong>If you experience difficulties with the credit card processing please call (888)888-8888 regarding this VPAY CLAIM PAYMENT.<br/><br/>*If you have any questions or require assistance with the claim, please call us at (888)888-8888 or email mdesai@vpayusa.com.</strong><br/>',
'1. No action is needed on your behalf.<br/><br/>2. If there are any problems with the receipt of this payment, please call the Optum Customer Service Center at 1-888-920-0592. Please have your Payment ID (located above) available for reference.<br/><br/>3. If you have questions about billing details or reimbursement amount, please contact UNITEDHEALTHCARE at 1-800-873-7242.');
