update virtual_card.VIRTUAL_CARD_DOCUMENT_CUSTOM_REMARKS
set custom_remarks1 = 'Electronic Provider Remittance Advice',
    custom_remarks2 = 'Advice of Payment - Virtual card',
    custom_remarks3 = 'The document you are holding is a payment for services provided. This document is sent on behalf of',
    custom_remarks4 = 'who has partnered with [Optum or Optum Financial] to provide a faster, more efficient way to reimburse your business for services rendered.<br/><p>To view the payment balance and all activity, please visit: <a href="https://payments.optum.com/">https://payments.optum.com</a></p>',
    custom_remarks5 = 'If there are any problems with the receipt of this payment, please call Customer Service Center at 1-XXX-XXX-XXXX. Please have your Payment ID (located above) available for reference.',
    custom_remarks6 = 'If you elect to accept payment by VCC, processing fees will be assessed at the rate outlined in your merchant agreement with your acquiring bank. If your organization prefers a different form of payment, please email [support email] or call [support phone number].'
WHERE org_id = '123';