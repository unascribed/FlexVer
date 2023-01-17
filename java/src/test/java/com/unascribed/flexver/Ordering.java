package com.unascribed.flexver;

import java.util.Arrays;
import java.util.NoSuchElementException;

public enum Ordering {
    LESS("<"),
    EQUAL("="),
    GREATER(">");

    public final String charRepresentation;

    Ordering(String charRepresentation) {
        this.charRepresentation = charRepresentation;
    }

    @Override
    public String toString() {
        return charRepresentation;
    }

    public Ordering invert() {
        switch (this) {
            case LESS:
                return Ordering.GREATER;
            case EQUAL:
                return this;
            case GREATER:
                return Ordering.LESS;
            default:
                throw new IllegalArgumentException();
        }
    }

    public static Ordering fromString(String str) {
        return Arrays.stream(Ordering.values())
            .filter(ord -> ord.charRepresentation.equals(str))
            .findFirst()
            .orElseThrow(() -> new NoSuchElementException("'"+str+"' is not a valid ordering"));
    }

    /**
     * Converts an integer returned by a method like {@link FlexVerComparator#compare(String, String)} to an {@link Ordering}
     */
    public static Ordering fromComparison(int i) {
        if (i < 0) return Ordering.LESS;
        if (i == 0) return Ordering.EQUAL;
        if (i > 0) return Ordering.GREATER;

        throw new AssertionError();
    }
}
