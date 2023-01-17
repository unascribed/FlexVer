require "spec"
require "../src/flexver.cr"

private def parse_test(path : Path) : Array(Tuple(String, Char, String))
  File.read(path)
    .split("\n")
    .reject(&.starts_with?("#"))
    .reject(&.blank?)
    .map do |line|
      words = line.split(" ")
      {words[0], words[1][0], words[2]}
    end
end

TEST_DIR = Path.new("../test")
TESTS = ["test_vectors.txt", "large.txt"]

describe FlexVer do
  tests = TESTS.map { |file| parse_test(TEST_DIR / file) } .sum

  tests.each do |a, cmp, b|
    it "performs sample comparasions correctly: #{a} #{cmp} #{b}" do
      cmp_int = case cmp
                when '>'
                  1
                when '<'
                  -1
                when '='
                  0
                end
      (FlexVer.new(a) <=> FlexVer.new(b)).clamp(-1 .. 1).should eq(cmp_int)
    end
  end

  it "handles re-composition correctly" do
    ver = FlexVer.new "0.17.1-beta.1"
    ver.to_s.should eq("0.17.1-beta.1")
  end
end
