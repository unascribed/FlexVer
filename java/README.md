# FlexVer-Java

A basic Java implementation of FlexVer, originally written for Quilt Loader.

## Getting it

You can either copy [FlexVerComparator.java](com/unascribed/flexver/FlexVerComparator.java) wholesale
into your project, or retrieve it from the Sleeping Town Maven, like so in Gradle:

```gradle
repositories {
	maven {
		url 'https://repo.sleeping.town'
		content.includeGroup 'com.unascribed'
	}
}

dependencies {
	implementation 'com.unascribed:flexver:1.0'
}
```

## Usage

The sole public method in this library is FlexVerComparator.compare. Simply pass it two strings:
```java
FlexVerComparator.compare("1.0", "1.1"); // -> -1
```
