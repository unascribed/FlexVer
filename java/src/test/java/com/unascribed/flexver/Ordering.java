package com.unascribed.flexver;

import java.util.Arrays;
import java.util.NoSuchElementException;

public enum Ordering {
    Less("<"),
    Equal("="),
    Greater(">");

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
            case Less:
                return Ordering.Greater;
            case Equal:
                return this;
            case Greater:
                return Ordering.Less;
            default:
                throw new IllegalArgumentException();
        }
    }

    public static Ordering fromStr(String str) {
        return Arrays.stream(Ordering.values())
            .filter(ord -> ord.charRepresentation.equals(str))
            .findFirst()
            .orElseThrow(() -> new NoSuchElementException("'"+str+"' is not a valid ordering"));
    }

    /**
     * Converts an integer returned by a method like {@link FlexVerComparator#compare(String, String)} to an {@link Ordering}
     */
    public static Ordering fromComparison(int i) {
        if (i < 0) return Ordering.Less;
        if (i == 0) return Ordering.Equal;
        if (i > 0) return Ordering.Greater;

        throw new IllegalStateException();
    }
}