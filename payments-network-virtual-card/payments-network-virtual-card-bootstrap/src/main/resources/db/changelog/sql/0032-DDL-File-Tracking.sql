CREATE TABLE virtual_card.file_tracking (
    correlation_id varchar(32) NULL,
	"835" varchar(500) NULL,
	eob varchar(500) NULL,
	funding varchar(500) NULL,
	ap varchar(500) NULL,
	c835 varchar(500) NULL,
	companion varchar(500) NULL,
	CONSTRAINT file_tracking_pkey PRIMARY KEY (correlation_id)
);