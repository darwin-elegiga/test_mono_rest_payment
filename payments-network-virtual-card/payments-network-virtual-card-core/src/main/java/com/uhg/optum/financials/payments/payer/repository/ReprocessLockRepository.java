package com.uhg.optum.financials.payments.payer.repository;

import com.uhg.optum.financials.payments.payer.model.ReprocessLock;
import jakarta.transaction.Transactional;
import org.springframework.data.jpa.repository.JpaRepository;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.query.Param;

public interface ReprocessLockRepository extends JpaRepository<ReprocessLock, String> {

	/**
	 * Atomic acquire: inserts a fresh row for an identifier never seen before, or flips
	 * an existing {@code AVAILABLE} row back to {@code IN_PROGRESS} — the {@code WHERE}
	 * on the conflict update is what makes this safe under concurrent callers; if the row
	 * is already {@code IN_PROGRESS}, the update is skipped and 0 rows are affected.
	 * Returns 1 if the lock was acquired, 0 if one is already held.
	 */
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Transactional
	@Query(value = """
			INSERT INTO virtual_card.reprocess_lock (identifier, status, triggered_at)
			VALUES (:identifier, 'IN_PROGRESS', now())
			ON CONFLICT (identifier) DO UPDATE
				SET status = 'IN_PROGRESS', triggered_at = now(), released_at = NULL
				WHERE reprocess_lock.status = 'AVAILABLE'
			""", nativeQuery = true)
	int tryAcquireLock(@Param("identifier") String identifier);

	/**
	 * A plain {@code UPDATE ... WHERE}, not a delete — safe to call even when no row
	 * exists yet for this identifier (every normal, non-reprocessed payment), since an
	 * {@code UPDATE} matching zero rows is a harmless no-op rather than an error.
	 */
	@Modifying(clearAutomatically = true, flushAutomatically = true)
	@Transactional
	@Query(value = """
			UPDATE virtual_card.reprocess_lock
			SET status = 'AVAILABLE', released_at = now()
			WHERE identifier = :identifier
			""", nativeQuery = true)
	void releaseLock(@Param("identifier") String identifier);

}
