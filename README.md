# TradeBuddy
[![](https://img.shields.io/badge/dynamic/xml?color=success&label=repo%20version&query=%2F%2FProject%2FPropertyGroup%2FVersion&url=https%3A%2F%2Fraw.githubusercontent.com%2Fstatus102%2FTradeBuddy%2Fmaster%2FTradeBuddy%2FTradeBuddy.csproj)](https://github.com/status102/TradeBuddy/raw/master/latest.zip)
[![](https://img.shields.io/github/v/release/status102/TradeBuddy.svg)](https://github.com/status102/TradeBuddy/releases/latest)

记录交易的内容并根据预先设定计算交易金额，不会自动填写进交易窗口。

Record the content of the transaction and calculate the transaction amount according to the preset, and will not automatically fill in the transaction window.

雇员出售列表中的道具如果有设置预期价格，单价绿色为大于等于预期价格，小于时则为红色。

在市场布告板搜索道具时，在物品名字处显示预期价格。

支持同一个道具配置多个数量不同的价格计算方案，不同方案间以半角分号进行分割。实际计算时按配置方案数量从大到小遍历匹配，数量整除时则使用该方案。

例：200/2;250/3，交易数量为4个时先判断3是否整除，3无法整除继续判断，2能整除则2匹配成功。

暂不支持多方案复合匹配，如交易数量为5个时匹配为2+3

## 使用方法

/tb 打开交易历史记录窗口，查看过往交易

![](https://github.com/status102/TradeBuddy/raw/master/Image/History.png)

/tb cfg 打开设置窗口设置物品的预期价格

![](https://github.com/status102/TradeBuddy/raw/master/Image/Setting.png)

## Todo

- [x] 雇员出售列表颜色标注区分是否低于预期价格
- [x] 市场板子标注预期的价格
- [x] 交易时即时查询交易物品价格
- [x] 对同一目标多次交易时，累积记录多次交易内容
- [x] 交易历史增加物品图标显示
- [ ] 交易数量支持多方案复合匹配
- [ ] 更换为通过数据包或者sig获取交易itemId

## Thanks

感谢獭爹、Loskh、zaevi、Yui以及幻想科技群群友

## 附

第一个稍微正经一点的项目，试着学习使用Git、版本控制、各种规范以及新玩意，依旧会很丑陋就是了
