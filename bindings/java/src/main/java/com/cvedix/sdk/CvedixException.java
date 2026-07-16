package com.cvedix.sdk;

/** Thrown when a native CVEDIX call fails. Message comes from {@code cvedix_last_error()}. */
public class CvedixException extends RuntimeException {
    public CvedixException(String operation) {
        super(operation + ": " + CvedixLibrary.INSTANCE.cvedix_last_error());
    }
}
