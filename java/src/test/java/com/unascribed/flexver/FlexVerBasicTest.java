package com.unascribed.flexver;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Stream;

import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;

public class FlexVerBasicTest {
	public static final String[] ENABLED_TESTS = { "test_vectors.txt", "large.txt" };

	@ParameterizedTest(name = "{0} {2} {1}")
	@MethodSource("getEqualityTests")
	public void testEquality(String a, String b, Ordering expected) {
		Ordering c = Ordering.fromComparison(FlexVerComparator.compare(a, b));
		Ordering c2 = Ordering.fromComparison(FlexVerComparator.compare(b, a));

		// When inverting the operands we're comparing, the ordering should be inverted too
		assertEquals(c2.invert(), c, "Comparison method violates its general contract! ("+a+" <=> "+b+" is not commutative)");

		assertEquals(expected, c, "Ordering.fromComparison produced "+a+" "+c+" "+b);
	}

	public static Stream<Arguments> getEqualityTests() throws IOException {
		Path testsFolder = Path.of("../test").toAbsolutePath();
		List<String> lines = new ArrayList<>();

		for (String test : ENABLED_TESTS) {
			lines.addAll(Files.readAllLines(testsFolder.resolve(test)));
		}

		return lines.stream()
			.filter(line -> !line.startsWith("#"))
			.filter(line -> !line.isEmpty())
			.map(line -> {
				String[] split = line.split(" ", -1);
				if (split.length != 3) throw new IllegalArgumentException("Line formatted incorrectly, expected 2 spaces: "+line);
				return Arguments.of(split[0], split[2], Ordering.fromStr(split[1]));
			});
	}
}
