package com.uhg.optum.financials.payments.payer.util;

import com.uhg.optum.financials.payments.payer.exception.BadRequestException;
import com.uhg.optum.financials.payments.payer.exception.InternalServerException;
import com.uhg.optum.financials.payments.payer.exception.NotFoundException;
import com.uhg.optum.financials.payments.payer.exception.UnauthorizedException;
import org.springframework.http.HttpStatus;
import org.springframework.http.HttpStatusCode;
import org.springframework.security.access.AccessDeniedException;

/**
 * Shared utility to convert a downstream HTTP status code into the appropriate typed
 * RuntimeException so that GlobalExceptionHandler can format GET error responses
 * consistently with errorCode / errorDescription / errorMessage.
 *
 * @author M Shai249
 */
public final class HttpExceptionUtil {

	private HttpExceptionUtil() {
		// utility class – no instances
	}

	/**
	 * Builds a typed exception from an {@link HttpStatusCode} and an error message.
	 * @param httpStatusCode downstream status code
	 * @param errorMessage human-readable message to include in the exception
	 * @return the matching typed {@link RuntimeException}
	 */
	public static RuntimeException buildException(HttpStatusCode httpStatusCode, String errorMessage) {
		HttpStatus httpStatus = HttpStatus.resolve(httpStatusCode.value());
		if (httpStatus == null) {
			return httpStatusCode.is5xxServerError() ? new InternalServerException(errorMessage)
					: new RuntimeException(errorMessage);
		}

		switch (httpStatus) {
			case BAD_REQUEST:
				return new BadRequestException(errorMessage);
			case UNAUTHORIZED:
				return new UnauthorizedException(errorMessage);
			case FORBIDDEN:
				return new AccessDeniedException(errorMessage);
			case NOT_FOUND:
				return new NotFoundException(errorMessage);
			default:
				return httpStatusCode.is5xxServerError() ? new InternalServerException(errorMessage)
						: new RuntimeException(errorMessage);
		}
	}

}
