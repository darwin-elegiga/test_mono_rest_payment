package com.uhg.optum.financials.payments.payer.exception;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.JsonMappingException;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.crossstore.ChangeSetPersister;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.validation.ObjectError;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ControllerAdvice;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.util.LinkedHashMap;
import java.util.Arrays;
import java.util.stream.Collectors;

import static com.uhg.optum.financials.payments.payer.constant.AppConstants.ERROR;

@Slf4j
@ControllerAdvice
@SuppressWarnings("NullableProblems")
public class GlobalExceptionHandler {

	private final LogUtility logUtility;

	public GlobalExceptionHandler(LogUtility logUtility) {
		this.logUtility = logUtility;
	}

	@ExceptionHandler({ HttpMessageNotReadableException.class })
	public ResponseEntity<Object> handleHttpMessageNotReadableException(HttpMessageNotReadableException exception) {
		Throwable mostSpecificCause = exception.getMostSpecificCause();
		if (mostSpecificCause instanceof JsonProcessingException jsonProcessingException) {
			return this.buildResponse(HttpStatus.BAD_REQUEST, this.toJsonErrorMessage(jsonProcessingException));
		}
		this.logUtility.logMessage("HttpMessageNotReadableException occurred for field: " + exception.getMessage(),
				ERROR, log);
		return this.buildResponse(HttpStatus.BAD_REQUEST, exception.getMessage());
	}

	private String toJsonErrorMessage(JsonProcessingException exception) {
		if (exception instanceof JsonMappingException jsonMappingException) {
			String propertyName = jsonMappingException.getPath()
				.stream()
				.map(JsonMappingException.Reference::getFieldName)
				.filter((fieldName) -> fieldName != null && !fieldName.isBlank())
				.collect(Collectors.joining("."));

			if (!propertyName.isBlank()) {
				return propertyName + " invalid. " + jsonMappingException.getOriginalMessage();
			}
		}

		String message = exception.getOriginalMessage();
		if (message == null || message.isBlank()) {
			message = exception.getMessage();
		}
		return "Invalid JSON request body: " + message;
	}

	@ExceptionHandler({ RuntimeException.class, InternalServerException.class })
	public ResponseEntity<Object> handleRuntimeException(RuntimeException exception) {
		this.logUtility.logMessage(
				"RuntimeException occurred: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(),
				ERROR, log);
		return this.buildResponse(HttpStatus.INTERNAL_SERVER_ERROR, exception.getMessage());
	}

	@ExceptionHandler({ ServiceUnavailableException.class })
	public ResponseEntity<Object> handleServiceUnavailableException(ServiceUnavailableException exception) {
		this.logUtility.logMessage(
				"ServiceUnavailableException: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(),
				ERROR, log);
		return this.buildResponse(HttpStatus.SERVICE_UNAVAILABLE, exception.getMessage());
	}

	@ExceptionHandler({ AccessDeniedException.class })
	public ResponseEntity<Object> handleForbiddenException(AccessDeniedException exception) {
		this.logUtility.logMessage(
				"AccessDeniedException: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(), ERROR,
				log);
		return this.buildResponse(HttpStatus.FORBIDDEN, exception.getMessage());
	}

	@ExceptionHandler({ UnauthorizedException.class })
	public ResponseEntity<Object> handleUnAuthorizeException(UnauthorizedException exception) {
		this.logUtility.logMessage(
				"UnauthorizedException: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(), ERROR,
				log);
		return this.buildResponse(HttpStatus.UNAUTHORIZED, exception.getMessage());
	}

	@ExceptionHandler({ BadRequestException.class })
	public ResponseEntity<Object> badRequest(BadRequestException exception) {
		this.logUtility.logMessage(
				"BadRequestException: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(), ERROR,
				log);
		return this.buildResponse(HttpStatus.BAD_REQUEST, exception.getMessage());
	}

	@ExceptionHandler({ NotFoundException.class })
	public ResponseEntity<Object> notFound(NotFoundException exception) {
		this.logUtility.logMessage(
				"NotFoundException: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(), ERROR,
				log);
		return this.buildResponse(HttpStatus.NOT_FOUND, exception.getMessage());
	}

	@ExceptionHandler({ MethodArgumentNotValidException.class })
	public ResponseEntity<Object> handleException(MethodArgumentNotValidException exception) {
		this.logUtility.logMessage("Exception: " + Arrays.toString(exception.getStackTrace()) + exception.getMessage(),
				ERROR, log);
		String errorMessage = exception.getBindingResult()
			.getAllErrors()
			.stream()
			.map(ObjectError::getDefaultMessage)
			.collect(Collectors.joining(".\n"));
		return this.buildResponse(HttpStatus.BAD_REQUEST, errorMessage);
	}

	@ExceptionHandler({ ChangeSetPersister.NotFoundException.class })
	public ResponseEntity<Object> handleUserNotFoundException(ChangeSetPersister.NotFoundException exception) {
		this.logUtility.logMessage("UserNotFoundException occurred: " + Arrays.toString(exception.getStackTrace()) + " "
				+ exception.getMessage(), ERROR, log);
		return this.buildResponse(HttpStatus.NOT_FOUND, exception.getMessage());
	}

	private ResponseEntity<Object> buildResponse(HttpStatus httpStatus, String errorMessage) {
		if (this.isGetRequest()) {
			return ResponseEntity.status(httpStatus).body(this.buildGetErrorResponse(httpStatus, errorMessage));
		}

		LinkedHashMap<String, Object> errorResponse = new LinkedHashMap<>();
		errorResponse.put("error", errorMessage);
		return ResponseEntity.status(httpStatus).body(errorResponse);
	}

	private LinkedHashMap<String, Object> buildGetErrorResponse(HttpStatus httpStatus, String errorMessage) {
		LinkedHashMap<String, Object> errorResponse = new LinkedHashMap<>();
		errorResponse.put("errorCode", String.valueOf(httpStatus.value()));
		errorResponse.put("errorDescription", httpStatus.getReasonPhrase());
		errorResponse.put("errorMessage", errorMessage);
		return errorResponse;
	}

	private boolean isGetRequest() {
		var requestAttributes = RequestContextHolder.getRequestAttributes();
		if (!(requestAttributes instanceof ServletRequestAttributes servletRequestAttributes)) {
			return false;
		}

		try {
			Object request = servletRequestAttributes.getClass()
				.getMethod("getRequest")
				.invoke(servletRequestAttributes);
			Object method = request.getClass().getMethod("getMethod").invoke(request);
			return method instanceof String && "GET".equalsIgnoreCase((String) method);
		}
		catch (ReflectiveOperationException exception) {
			this.logUtility.logMessage(
					"Unable to determine request method for exception handling: " + exception.getMessage(), ERROR, log);
			return false;
		}
	}

}
