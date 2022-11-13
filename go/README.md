# flexver

[![Go Reference](https://pkg.go.dev/badge/github.com/unascribed/FlexVer/go.svg)](https://pkg.go.dev/github.com/unascribed/FlexVer/go/flexver)

A Go implementation of FlexVer.

## Getting it

Use `go get` to download the library:

```bash
go get -u github.com/unascribed/FlexVer/go/flexver
```

Then import the module in your Go code with `import "github.com/unascribed/FlexVer/go/flexver"`

## Usage

See the [documentation at pkg.go.dev](https://pkg.go.dev/github.com/unascribed/FlexVer/go/flexver)! In most cases, you'll want to use the `Compare` function to compare two versions:

```go
fmt.Println(flexver.Compare("1.0.1", "1.0.0"))
// Output: 1
```

For various [sorting functions](https://pkg.go.dev/sort) provided by the standard library (and [generic versions from `golang.org/x/exp/slices`](https://pkg.go.dev/golang.org/x/exp/slices#SortFunc)) a `Less` function is provided, complying with the contract those functions require.