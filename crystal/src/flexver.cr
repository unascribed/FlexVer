# Reimplementing this because the Crystal standard
# library `compare` method does not account for UTF-8
private def compare(a : String, b : String) : Int32
  a.chars <=> b.chars
end

# Conforms to FlexVer 1.0.1_09
#
# See: [https://github.com/unascribed/FlexVer/](https://github.com/unascribed/FlexVer/)
#
# Differs quite greatly from the standard library's `SemanticVersion` as there
# is no single way to represent a FlexVer - only a `parse`-type method is
# implemented as the initializer (see: `new`).
#
# ```
# FlexVer.new "b1.7.3" > FlexVer.new "a1.2.6" # => true
# FlexVer.new "0.17.1-beta.1" > FlexVer.new "0.17.1-beta.2" # => false
# # ...
# ```
struct FlexVer
  include Comparable(self)

  getter components : Array(Component)

  private enum ComponentType
    # The run is entirely non-digit codepoints
    Textual
    # The run is entirely digit codepoints
    Numeric
    # The run's first codepoint is ASCII hyphen-minus (`-`) **and is longer than one codepoint**
    PreRelease
    # The run's first codepoint is ASCII plus (`+`)
    Appendix
    # Represents blank padding spots used when two versions' lengths do not match.
    Null
  end

  # Represents a [component](https://github.com/unascribed/FlexVer/blob/trunk/SPEC.md#decomposition)
  # after decomposition; see `ComponentType`.
  private struct Component
    include Comparable(self)

    getter str : String
    getter type : ComponentType

    # Initializing with an empty string (done so by default) will return
    # a component of type `ComponentType::Null`.
    def initialize(@str : String = "")
      case @str[0]?
      when Nil
        @type = ComponentType::Null
      when .ascii_number?
        @type = ComponentType::Numeric
      when '-'
        if @str.size > 1
          @type = ComponentType::PreRelease
        else
          @type = ComponentType::Textual
        end
      when '+'
        @type = ComponentType::Appendix
      else
        @type = ComponentType::Textual
      end
    end

    # The comparison operator.
    #
    # Returns `-1`, `0` or `1` depending on whether `self` is considered lower than _other_'s, equal to _other_'s version or greater than _other_.
    #
    # See: [https://github.com/unascribed/FlexVer/blob/trunk/SPEC.md#comparison](https://github.com/unascribed/FlexVer/blob/trunk/SPEC.md#comparison)
    def <=>(other : self) : Int32
      # Done the ugly way because of a Crystal bug?
      #
      # when value
      # case true
      #   # this code never runs!
      # end
      #
      # This prevents us from using `case ... in`, hence the ugly `else` case at the end

      case
      when type == ComponentType::Null
        other.type == ComponentType::PreRelease ? 1 : -1
      when type == ComponentType::PreRelease && other.type == ComponentType::Null
        -1
      when type == ComponentType::Textual, type == ComponentType::PreRelease, type != other.type
        # Textual comparison
        str.chars <=> other.str.chars
      when type == ComponentType::Numeric
        # Numeric comparison
        # Implemented as described under "Codepoint-wise"
        a = str.lstrip('0').chars
        b = other.str.lstrip('0').chars
        if a.size != b.size
          return a.size <=> b.size end
        a <=> b
      else
        raise "#{type} ? #{other.type}"
      end
    end

    def to_s(io : IO) : Nil
      io << str
    end
  end

  private def get_component_split_type(c : Char)
    case c
    when '-', '+'
      0
    when .ascii_number?
      1
    else
      2
    end
  end

  # Parses `version_str` and creates a version object.
  def initialize(version_str : String)
    chunked = version_str.chars.chunks { |n| get_component_split_type(n) }
    components = chunked
      # Merge pre-releases as outlined in #12 - hacky, but works
      .reduce([] of String) do |sum, c|
        str = c[1].join
        if sum[-1]?.try &.starts_with?('-') && !str[0].ascii_number?
          sum[-1] = sum[-1] + str
        else
          sum << str
        end
        sum
      end
      .map { |v| Component.new(v) }
    @components = components.take_while { |c| c.type != ComponentType::Appendix }
  end

  # The comparison operator.
  #
  # Returns `-1`, `0` or `1` depending on whether `self`'s version is lower than _other_'s, equal to _other_'s version or greater than _other_'s version.
  #
  # ```
  # ver1 = FlexVer.new "b1.7.3"
  # ver2 = FlexVer.new "a1.2.6"
  #
  # ver1 <=> ver2 # => 1
  # ver1 <=> ver1 # => 0
  # ver2 <=> ver1 # => -1
  # ```
  def <=>(other : self) : Int32
    (0 .. (Math.max(components.size, other.components.size) - 1)).each do |i|
      a = components[i]?
      b = other.components[i]?

      # default to nulls
      c = (a || Component.new) <=> (b || Component.new)

      if c != 0
        return c end
    end
    components <=> other.components
  end

  # Returns the string representation of this version
  #
  # NOTE: This will get rid of appendices (`+foo`), so it is not guaranteed
  # to be the exact same string passed into `new`.
  #
  # ```
  # ver = FlexVer.new "0.17.1-beta.1"
  # ver.to_s # => "0.17.1-beta.1"
  # ```
  def to_s(io : IO) : Nil
    components.each do |component|
      component.to_s(io)
    end
  end
end
