package com.uhg.optum.financials.payments.payer.exception;

import com.fasterxml.jackson.core.JsonParseException;
import com.fasterxml.jackson.databind.JsonMappingException;
import com.uhg.optum.financials.payments.payer.util.LogUtility;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;
import org.springframework.data.crossstore.ChangeSetPersister;
import org.springframework.http.HttpStatus;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.security.access.AccessDeniedException;
import org.springframework.validation.BindingResult;
import org.springframework.validation.ObjectError;
import org.springframework.web.bind.MethodArgumentNotValidException;

import java.util.Map;

import static org.assertj.core.api.AssertionsForInterfaceTypes.assertThat;

@SuppressWarnings("unchecked")
class GlobalExceptionHandlerTests {

	private final LogUtility logUtility = Mockito.mock(LogUtility.class);

	private final GlobalExceptionHandler globalExceptionHandler = new GlobalExceptionHandler(this.logUtility);

	private Map<String, String> bodyAsMap(Object body) {
		assertThat(body).isNotNull();
		return (Map<String, String>) body;
	}

	@Test
	void handleHttpMessageNotReadableException() {
		HttpMessageNotReadableException exception = Mockito.mock(HttpMessageNotReadableException.class);
		assertThat(this.globalExceptionHandler.handleHttpMessageNotReadableException(exception).getStatusCode())
			.isEqualTo(HttpStatus.BAD_REQUEST);
	}

	@Test
	void handleHttpMessageNotReadableException_withJsonParseCause_returnsSpecificParseMessage() {
		HttpMessageNotReadableException exception = Mockito.mock(HttpMessageNotReadableException.class);
		JsonParseException jsonParseException = Mockito.mock(JsonParseException.class);
		Mockito.when(exception.getMostSpecificCause()).thenReturn(jsonParseException);
		Mockito.when(jsonParseException.getOriginalMessage()).thenReturn("Unexpected character ('}' at position 12)");

		var response = this.globalExceptionHandler.handleHttpMessageNotReadableException(exception);
		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
		assertThat(bodyAsMap(response.getBody()).get("error"))
			.isEqualTo("Invalid JSON request body: Unexpected character ('}' at position 12)");
	}

	@Test
	void testHttpMessageNotReadableJsonMappingException() {
		HttpMessageNotReadableException exception = Mockito.mock(HttpMessageNotReadableException.class);
		JsonMappingException jsonMappingException = Mockito.mock(JsonMappingException.class);
		Mockito.when(exception.getMostSpecificCause()).thenReturn(jsonMappingException);
		Mockito.when(jsonMappingException.getPath())
			.thenReturn(java.util.List.of(new JsonMappingException.Reference(new Object(), "bin")));
		Mockito.when(jsonMappingException.getOriginalMessage())
			.thenReturn("Cannot deserialize value of type `java.lang.Long` from String");

		var response = this.globalExceptionHandler.handleHttpMessageNotReadableException(exception);
		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
		assertThat(bodyAsMap(response.getBody()).get("error"))
			.isEqualTo("bin invalid. Cannot deserialize value of type `java.lang.Long` from String");
	}

	@Test
	void testHandleRuntimeException() {
		RuntimeException exception = new RuntimeException("runtime error");
		assertThat(this.globalExceptionHandler.handleRuntimeException(exception).getStatusCode())
			.isEqualTo(HttpStatus.INTERNAL_SERVER_ERROR);
	}

	@Test
	void testHandleServiceUnavailableException() {
		ServiceUnavailableException exception = new ServiceUnavailableException("Queue unavailable",
				new RuntimeException());
		assertThat(this.globalExceptionHandler.handleServiceUnavailableException(exception).getStatusCode())
			.isEqualTo(HttpStatus.SERVICE_UNAVAILABLE);
	}

	@Test
	void testHandleUnAuthorizeException() {
		UnauthorizedException exception = new UnauthorizedException("unauthorized");
		assertThat(this.globalExceptionHandler.handleUnAuthorizeException(exception).getStatusCode())
			.isEqualTo(HttpStatus.UNAUTHORIZED);
	}

	@Test
	void testHandleAccessDeniedException() {
		AccessDeniedException exception = new AccessDeniedException("forbidden");
		assertThat(this.globalExceptionHandler.handleForbiddenException(exception).getStatusCode())
			.isEqualTo(HttpStatus.FORBIDDEN);
	}

	@Test
	void testBadRequest() {
		BadRequestException exception = new BadRequestException("bad request");
		assertThat(this.globalExceptionHandler.badRequest(exception).getStatusCode()).isEqualTo(HttpStatus.BAD_REQUEST);
	}

	@Test
	void testHandleException() {
		MethodArgumentNotValidException exception = Mockito.mock(MethodArgumentNotValidException.class);
		BindingResult bindingResult = Mockito.mock(BindingResult.class);
		Mockito.when(exception.getBindingResult()).thenReturn(bindingResult);
		assertThat(this.globalExceptionHandler.handleException(exception).getStatusCode())
			.isEqualTo(HttpStatus.BAD_REQUEST);
	}

	@Test
	void testNotFound() {
		NotFoundException notFoundException = new NotFoundException("not found");
		assertThat(this.globalExceptionHandler.notFound(notFoundException).getStatusCode())
			.isEqualTo(HttpStatus.NOT_FOUND);
	}

	@Test
	void testNotFound_returnsStructuredErrorResponse() {
		NotFoundException notFoundException = new NotFoundException("not found");

		var response = this.globalExceptionHandler.notFound(notFoundException);
		assertThat(response.getStatusCode()).isEqualTo(HttpStatus.NOT_FOUND);
		assertThat(bodyAsMap(response.getBody()).get("error")).isEqualTo("not found");
	}

	@Test
	void testHandleUserNotFoundException() {
		ChangeSetPersister.NotFoundException userNotFoundException = Mockito
			.mock(ChangeSetPersister.NotFoundException.class);
		Mockito.when(userNotFoundException.getMessage()).thenReturn("user not found");
		assertThat(this.globalExceptionHandler.handleUserNotFoundException(userNotFoundException).getStatusCode())
			.isEqualTo(HttpStatus.NOT_FOUND);
	}

	@Test
	void testHandleException_withMultipleErrors() {
		MethodArgumentNotValidException exception = Mockito.mock(MethodArgumentNotValidException.class);
		BindingResult bindingResult = Mockito.mock(BindingResult.class);
		ObjectError error1 = new ObjectError("object1", "error message 1");
		ObjectError error2 = new ObjectError("object2", "error message 2");
		Mockito.when(exception.getBindingResult()).thenReturn(bindingResult);
		Mockito.when(bindingResult.getAllErrors()).thenReturn(java.util.Arrays.asList(error1, error2));
		var response = this.globalExceptionHandler.handleException(exception);
		assertThat(bodyAsMap(response.getBody()).get("error")).isEqualTo("error message 1.\nerror message 2");
	}

}
