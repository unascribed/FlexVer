require "spec"
require "../src/flexver.cr"

describe FlexVer do
  [
    {"b1.7.3", '>', "a1.2.6"},
    {"a1.1.2", '<', "a1.1.2_01"},
    {"1.16.5-0.00.5", '>', "1.14.2-1.3.7"},
    {"1.0.0", '<', "1.0.0_01"},
    {"1.0.1", '>', "1.0.0_01"},
    {"0.17.1-beta.1", '<', "0.17.1"},
    {"0.17.1-beta.1", '<', "0.17.1-beta.2"},
    {"1.4.5_01", '=', "1.4.5_01+exp-1.17"},
    {"1.4.5_01", '=', "1.4.5_01+exp-1.17-moretext"},
    {"14w16a", '<', "18w40b"},
    {"18w40a", '<', "18w40b"},
    {"0.6.0-1.18.x", '<', "0.9.beta-1.18.x"},
    {"1.0", '<', "1.1"},
    {"1.0", '<', "1.0.1"},
    {"10", '>', "2"},

    # codepoint-wise numeric comparasion check
    {"36893488147419103232", '<', "36893488147419103233"},

    # nonsense comparasions
    {"1.4.5_01+exp-1.17", '<', "18w40b"},
    {"13w02a", '<', "c0.3.0_01"},
  ].each do |a, cmp, b|
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
