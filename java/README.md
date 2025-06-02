# FlexVer-Java

The original Java implementation of FlexVer, initially written for Quilt Loader.

## Getting it

You can either copy [FlexVerComparator.java](src/main/java/com/unascribed/flexver/FlexVerComparator.java)
wholesale into your project, or retrieve it from Maven Central, like so in Gradle:

```gradle
repositories {
	mavenCentral()
}

dependencies {
	implementation 'com.unascribed:flexver-java:1.1.1'
}
```

(Releases are also published to the Sleeping Town Maven at repo.sleeping.town)

## Usage

The sole public method in this library is FlexVerComparator.compare. Simply pass it two strings:
```java
FlexVerComparator.compare("1.0", "1.1"); // -> -1
```
