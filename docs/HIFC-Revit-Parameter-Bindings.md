# HIFC 参数绑定 Revit 构件核对表

> 生成依据：`docs/HIFC.txt`、`docs/mini.txt` 与 `src/PlugHub.HubeiReportParameters` 当前精确映射逻辑。

## 精确绑定规则

| IFC 构件 | HIFC 语义 | Revit 绑定构件 |
| --- | --- | --- |
| `IfcProject` | 项目 | 项目信息（`OST_ProjectInformation`） |
| `IfcBuilding` | 建筑/楼栋 | 项目信息（`OST_ProjectInformation`） |
| `IfcBuildingStorey` | 楼层 | 标高（`OST_Levels`） |
| `IfcSpace` | 空间 | 房间（`OST_Rooms`）、空间（`OST_MEPSpaces`）、面积（`OST_Areas`） |
| `IfcSpatialZone` | 区域 | 房间（`OST_Rooms`）、空间（`OST_MEPSpaces`）、面积（`OST_Areas`） |
| `IfcSite` | 场地/总图 | 场地（`OST_Site`） |
| `IfcSlab` | 板类构件 | 楼板（`OST_Floors`） |

说明：用户界面的“全局/总图/单体”决定创建哪个分类的参数；“最小报建”是参数范围过滤。勾选“总图 + 最小报建”只创建 mini 中属于总图分类的参数，勾选“单体 + 最小报建”只创建 mini 中属于单体分类的参数。实际 Revit 绑定类别由参数来源的 IFC 构件决定，并回查 HIFC 的 Pset/IFC 映射，不再按 Pset 名称把参数扩散到整类构件。

## HIFC 参数统计

- HIFC 原始属性行数（按 `Pset + 参数名` 去重）：315
- HIFC 实际创建共享参数数（按参数名去重）：215
- `全局` 分类参数数：76
- `总图` 分类参数数：34
- `单体` 分类参数数：127
- 类型统计：文字 114，数值 62，整数 22，布尔 17

## 最小报建统计

- mini 原始属性行数（按 `Pset + 参数名` 去重）：72
- mini 实际创建共享参数数（按参数名去重）：64
- 与 HIFC 同 Pset 同名精确匹配行数：66
- 未同名但继承同 Pset IFC 映射行数：6
- mini `全局` 分类参数数：2
- mini `总图` 分类参数数：19
- mini `单体` 分类参数数：44

## HIFC 涉及的 IFC 构件

| IFC 构件 | Pset 数 | 去重参数数 | Revit 绑定构件 |
| --- | ---: | ---: | --- |
| IfcProject | 7 | 76 | 项目信息(OST_ProjectInformation) |
| IfcBuilding | 3 | 59 | 项目信息(OST_ProjectInformation) |
| IfcBuildingStorey | 2 | 17 | 标高(OST_Levels) |
| IfcSpace | 4 | 21 | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) |
| IfcSpatialZone | 4 | 45 | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) |
| IfcSite | 22 | 34 | 场地(OST_Site) |
| IfcSlab | 6 | 15 | 楼板(OST_Floors) |

## 重名参数

HIFC 中共 29 个参数名在多个 Pset 中重复出现，插件按同名共享参数合并 IFC/Revit 绑定目标。

| 参数名 | 出现次数 | IFC 构件 | Revit 绑定构件 | 来源 Pset |
| --- | ---: | --- | --- | --- |
| 编号 | 2 | IfcProject(项目)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_报建信息属性集、Pset_规划控制线信息属性集 |
| 建筑物编码 | 2 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集、Pset_登记信息属性集 |
| 总建筑面积 | 2 | IfcProject(项目)、IfcSpace(空间) | 项目信息(OST_ProjectInformation)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_登记信息属性集、Pset_建筑空间信息属性集 |
| 建筑状态 | 2 | IfcProject(项目)、IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集、Pset_建筑技术信息属性集 |
| 备注 | 5 | IfcProject(项目)、IfcSite(场地)、IfcBuildingStorey(楼层)、IfcSpace(空间)、IfcSpatialZone(区域) | 项目信息(OST_ProjectInformation)、场地(OST_Site)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_区划信息属性集、Pset_申报信息属性集、Pset_总平构件信息属性集、Pset_建筑楼层信息属性集、Pset_停车位信息属性集 |
| 建筑密度 | 3 | IfcProject(项目)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_项目控制指标信息属性集、Pset_场地信息属性集、Pset_总平构件信息属性集 |
| 容积率 | 3 | IfcProject(项目)、IfcSite(场地)、IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_项目控制指标信息属性集、Pset_场地信息属性集、Pset_建筑技术信息属性集 |
| 绿地率 | 3 | IfcProject(项目)、IfcSite(场地)、IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_项目控制指标信息属性集、Pset_场地信息属性集、Pset_建筑技术信息属性集 |
| 长度 | 2 | IfcSite(场地)、IfcSlab(板类) | 场地(OST_Site)、楼板(OST_Floors) | Pset_场地信息属性集、Pset_阳台信息属性集 |
| 投影面积 | 23 | IfcSite(场地)、IfcBuilding(建筑/楼栋)、IfcSpatialZone(区域)、IfcSlab(板类) | 场地(OST_Site)、项目信息(OST_ProjectInformation)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_规划总用地信息属性集、Pset_规划净用地信息属性集、Pset_其它用地信息属性集、Pset_服务设施信息属性集、Pset_儿童老年人活动场地信息属性集、Pset_居民健身场地信息属性集、Pset_消防回车场信息属性集、Pset_消防分区信息属性集、Pset_人防区域信息属性集、Pset_堆场信息属性集、Pset_构筑物信息属性集、Pset_总平构件信息属性集、Pset_消防场地信息属性集、Pset_绿地信息属性集、Pset_建筑技术信息属性集、Pset_停车场信息属性集、Pset_室内停车场信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 名称 | 16 | IfcSite(场地) | 场地(OST_Site) | Pset_规划总用地信息属性集、Pset_规划净用地信息属性集、Pset_其它用地信息属性集、Pset_服务设施信息属性集、Pset_儿童老年人活动场地信息属性集、Pset_居民健身场地信息属性集、Pset_消防回车场信息属性集、Pset_消防分区信息属性集、Pset_人防区域信息属性集、Pset_堆场信息属性集、Pset_构筑物信息属性集、Pset_消防场地信息属性集、Pset_规划控制线信息属性集、Pset_道路红线信息属性集、Pset_道路中心线信息属性集、Pset_道路转角视距红线信息属性集 |
| 构件类型 | 2 | IfcSite(场地)、IfcSpatialZone(区域) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_总平构件信息属性集、Pset_室内停车场信息属性集 |
| 计算系数 | 8 | IfcSite(场地)、IfcSpatialZone(区域)、IfcSlab(板类) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_总平构件信息属性集、Pset_建筑区域信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 计容系数 | 8 | IfcSite(场地)、IfcSpatialZone(区域)、IfcSlab(板类) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_总平构件信息属性集、Pset_建筑区域信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 折算系数 | 2 | IfcSite(场地)、IfcSpace(空间)、IfcSpatialZone(区域) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_绿地信息属性集、Pset_停车位信息属性集 |
| 道路宽度 | 3 | IfcSite(场地) | 场地(OST_Site) | Pset_道路边界线信息属性集、Pset_道路中心线信息属性集、Pset_道路转角视距红线信息属性集 |
| 道路类型 | 2 | IfcSite(场地) | 场地(OST_Site) | Pset_道路红线信息属性集、Pset_道路中心线信息属性集 |
| 建筑面积 | 2 | IfcBuilding(建筑/楼栋)、IfcSpatialZone(区域) | 项目信息(OST_ProjectInformation)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑技术信息属性集、Pset_建筑区域信息属性集 |
| 人数 | 3 | IfcBuilding(建筑/楼栋)、IfcBuildingStorey(楼层)、IfcSpace(空间) | 项目信息(OST_ProjectInformation)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑技术信息属性集、Pset_建筑楼层信息属性集、Pset_居住建筑空间信息属性集 |
| 主功能类别 | 2 | IfcBuildingStorey(楼层)、IfcSpatialZone(区域) | 标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑楼层信息属性集、Pset_建筑区域信息属性集 |
| 子功能类别 | 2 | IfcBuildingStorey(楼层)、IfcSpatialZone(区域) | 标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑楼层信息属性集、Pset_建筑区域信息属性集 |
| 计算标高 | 2 | IfcBuildingStorey(楼层)、IfcSpatialZone(区域) | 标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑楼层信息属性集、Pset_建筑区域信息属性集 |
| 高度 | 2 | IfcSpace(空间)、IfcSlab(板类) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_建筑空间信息属性集、Pset_阳台信息属性集 |
| 层数 | 2 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集、Pset_室内停车场信息属性集 |
| 公建车位 | 2 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集、Pset_停车场信息属性集 |
| 公建大类 | 7 | IfcSpatialZone(区域)、IfcSlab(板类) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_室内停车场信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 公建小类 | 7 | IfcSpatialZone(区域)、IfcSlab(板类) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_室内停车场信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 间距审查 | 5 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集 |
| 退让审查 | 5 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集 |

## HIFC 全量参数核对表

| 参数名 | 类型 | 分类 | IFC 构件 | Revit 绑定构件 | 来源 Pset |
| --- | --- | --- | --- | --- | --- |
| 阶段 | 文字 | 全局、总图、单体 | IfcProject(项目)、IfcBuilding(建筑/楼栋)、IfcBuildingStorey(楼层)、IfcSpace(空间)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、场地(OST_Site) | Pset_Manifest |
| 应用模型精度 | 数值 | 全局、总图、单体 | IfcProject(项目)、IfcBuilding(建筑/楼栋)、IfcBuildingStorey(楼层)、IfcSpace(空间)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、场地(OST_Site) | Pset_Manifest |
| 实际模型精度 | 数值 | 全局、总图、单体 | IfcProject(项目)、IfcBuilding(建筑/楼栋)、IfcBuildingStorey(楼层)、IfcSpace(空间)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、场地(OST_Site) | Pset_Manifest |
| 手机号码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 编号 | 文字 | 全局、总图 | IfcProject(项目)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_报建信息属性集、Pset_规划控制线信息属性集 |
| 姓 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 名 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 邮箱地址 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 公司 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 公司性质 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 部门 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 国家 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 城镇 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 街道 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 通信地址 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 国家编码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 邮政编码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_报建信息属性集 |
| 宗地数量 | 整数 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 宗地用途 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 宗地编号 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 宗地信息 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 宗地质量等级 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 宗地权力 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 宗地权利人 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集 |
| 建筑物编码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_地籍信息属性集、Pset_登记信息属性集 |
| 建设性质 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 总建筑面积 | 数值 | 全局、单体 | IfcProject(项目)、IfcSpace(空间) | 项目信息(OST_ProjectInformation)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_登记信息属性集、Pset_建筑空间信息属性集 |
| 现状名称 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 审批名称 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 详细地址 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 不动产地址 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 建成时间 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 建筑状态 | 文字 | 全局、单体 | IfcProject(项目)、IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集、Pset_建筑技术信息属性集 |
| 建设性质代码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_登记信息属性集 |
| 所属省级行政区 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 省级行政区代码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 所属地级行政区 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 地级行政区代码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 所属县级行政区 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 县级行政区代码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 所属乡级行政区 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 乡级行政区代码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 所属村(社区) | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 村(社区)代码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 网格编码 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 所属小区 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_区划信息属性集 |
| 备注 | 文字 | 全局、总图、单体 | IfcProject(项目)、IfcSite(场地)、IfcBuildingStorey(楼层)、IfcSpace(空间)、IfcSpatialZone(区域) | 项目信息(OST_ProjectInformation)、场地(OST_Site)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_区划信息属性集、Pset_申报信息属性集、Pset_总平构件信息属性集、Pset_建筑楼层信息属性集、Pset_停车位信息属性集 |
| 基点坐标X | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 基点坐标Y | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 基点高程 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 项目编号 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 项目名称 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 建设单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 项目地址 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 设计单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 设计人 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 勘察单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 咨询单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 施工单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 监理单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 图审单位 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 坐标系名称 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 高程系名称 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 经度 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 纬度 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 |
| 建筑密度 | 数值 | 全局、总图 | IfcProject(项目)、IfcSite(场地) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_项目控制指标信息属性集、Pset_场地信息属性集、Pset_总平构件信息属性集 |
| 容积率 | 数值 | 全局、总图、单体 | IfcProject(项目)、IfcSite(场地)、IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_项目控制指标信息属性集、Pset_场地信息属性集、Pset_建筑技术信息属性集 |
| 绿地率 | 数值 | 全局、总图、单体 | IfcProject(项目)、IfcSite(场地)、IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation)、场地(OST_Site) | Pset_项目控制指标信息属性集、Pset_场地信息属性集、Pset_建筑技术信息属性集 |
| 地块名称 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 建筑限高 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 海拔限高 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 机动车位 | 整数 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 非机动车车位 | 整数 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 非机动车类型 | 文字 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 非机动车折算系数 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 配置套值 | 数值 | 全局 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_项目控制指标信息属性集 |
| 长度 | 数值 | 总图、单体 | IfcSite(场地)、IfcSlab(板类) | 场地(OST_Site)、楼板(OST_Floors) | Pset_场地信息属性集、Pset_阳台信息属性集 |
| 宽度 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 总用地面积 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 可建设用地面积 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 建筑比例 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 混合用地比例 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 地块编号 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 城市用地分类和使用 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 |
| 投影面积 | 数值 | 总图、单体 | IfcSite(场地)、IfcBuilding(建筑/楼栋)、IfcSpatialZone(区域)、IfcSlab(板类) | 场地(OST_Site)、项目信息(OST_ProjectInformation)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_规划总用地信息属性集、Pset_规划净用地信息属性集、Pset_其它用地信息属性集、Pset_服务设施信息属性集、Pset_儿童老年人活动场地信息属性集、Pset_居民健身场地信息属性集、Pset_消防回车场信息属性集、Pset_消防分区信息属性集、Pset_人防区域信息属性集、Pset_堆场信息属性集、Pset_构筑物信息属性集、Pset_总平构件信息属性集、Pset_消防场地信息属性集、Pset_绿地信息属性集、Pset_建筑技术信息属性集、Pset_停车场信息属性集、Pset_室内停车场信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 名称 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_规划总用地信息属性集、Pset_规划净用地信息属性集、Pset_其它用地信息属性集、Pset_服务设施信息属性集、Pset_儿童老年人活动场地信息属性集、Pset_居民健身场地信息属性集、Pset_消防回车场信息属性集、Pset_消防分区信息属性集、Pset_人防区域信息属性集、Pset_堆场信息属性集、Pset_构筑物信息属性集、Pset_消防场地信息属性集、Pset_规划控制线信息属性集、Pset_道路红线信息属性集、Pset_道路中心线信息属性集、Pset_道路转角视距红线信息属性集 |
| 版本 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_规划净用地信息属性集 |
| 用地类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_其它用地信息属性集 |
| 构件类型 | 文字 | 总图、单体 | IfcSite(场地)、IfcSpatialZone(区域) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_总平构件信息属性集、Pset_室内停车场信息属性集 |
| 计算系数 | 数值 | 总图、单体 | IfcSite(场地)、IfcSpatialZone(区域)、IfcSlab(板类) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_总平构件信息属性集、Pset_建筑区域信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 计容系数 | 数值 | 总图、单体 | IfcSite(场地)、IfcSpatialZone(区域)、IfcSlab(板类) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_总平构件信息属性集、Pset_建筑区域信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 所属建筑编号 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_消防场地信息属性集 |
| 绿地类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_绿地信息属性集 |
| 折算系数 | 数值 | 总图、单体 | IfcSite(场地)、IfcSpace(空间)、IfcSpatialZone(区域) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_绿地信息属性集、Pset_停车位信息属性集 |
| 区内道路类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_道路边界线信息属性集 |
| 道路宽度 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_道路边界线信息属性集、Pset_道路中心线信息属性集、Pset_道路转角视距红线信息属性集 |
| 红线类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_规划控制线信息属性集 |
| 边界线编号 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_边界分段线信息属性集 |
| 边界类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_边界分段线信息属性集 |
| 界外类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_边界分段线信息属性集 |
| 道路类型 | 文字 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_道路红线信息属性集、Pset_道路中心线信息属性集 |
| 红线宽度 | 数值 | 总图 | IfcSite(场地) | 场地(OST_Site) | Pset_道路红线信息属性集 |
| 耐火等级 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑类型 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 地上建筑层数 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 地下建筑层数 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑占地面积 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑面积 | 数值 | 单体 | IfcBuilding(建筑/楼栋)、IfcSpatialZone(区域) | 项目信息(OST_ProjectInformation)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑技术信息属性集、Pset_建筑区域信息属性集 |
| 登记用途 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑用途 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 抗震性能等级 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑体积 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 人数 | 整数 | 单体 | IfcBuilding(建筑/楼栋)、IfcBuildingStorey(楼层)、IfcSpace(空间) | 项目信息(OST_ProjectInformation)、标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑技术信息属性集、Pset_建筑楼层信息属性集、Pset_居住建筑空间信息属性集 |
| 地下建筑高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 室内外高差最大值 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑名称 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑类型名称 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 登记用途名称 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑用途名称 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 抗震设防类别 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 安全等级 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑类别 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑与设施用途分类 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 设计使用年限 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 停车位数量 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 建筑编号 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 规划高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 地坪标高 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 户数 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 保障房户数 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 消防高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 檐口高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 屋脊高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 海拔高度 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 室外地坪标高 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 临时永久 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 结构类型 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 形状 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 山墙开窗 | 文字 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 民用建筑 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 装配式奖励面积 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 |
| 人均集中绿地面积 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 地面停车位数量与住宅总套数的比率 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 地面停车占地面积与其总建设用地面积的比率 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 是否设置能源管理系统 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 是否设置分类、分级用能自动远传计量 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 是否设置空气质量监测系统 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 是否设置用水远传计量系统 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 是否设置水质在线监测系统 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 是否具有智能化服务系统 | 布尔 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 平均日用水量 | 数值 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 地面停车位数量 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 住宅总套数 | 整数 | 单体 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_绿色建筑信息属性集 |
| 底标高 | 数值 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 主功能类别 | 文字 | 单体 | IfcBuildingStorey(楼层)、IfcSpatialZone(区域) | 标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑楼层信息属性集、Pset_建筑区域信息属性集 |
| 子功能类别 | 文字 | 单体 | IfcBuildingStorey(楼层)、IfcSpatialZone(区域) | 标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑楼层信息属性集、Pset_建筑区域信息属性集 |
| 楼层类型 | 文字 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 建筑层高 | 数值 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 计算标高 | 数值 | 单体 | IfcBuildingStorey(楼层)、IfcSpatialZone(区域) | 标高(OST_Levels)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑楼层信息属性集、Pset_建筑区域信息属性集 |
| 楼层面积 | 数值 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 是否设置避难层 | 布尔 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 楼层名称 | 文字 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 楼层编号 | 文字 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 楼层属性信息 | 文字 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 建筑基底 | 数值 | 单体 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 |
| 空间功能 | 文字 | 单体 | IfcSpace(空间) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑空间信息属性集 |
| 功能 | 文字 | 单体 | IfcSpace(空间) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑空间信息属性集 |
| 高度 | 数值 | 单体 | IfcSpace(空间)、IfcSlab(板类) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_建筑空间信息属性集、Pset_阳台信息属性集 |
| 净面积 | 数值 | 单体 | IfcSpace(空间) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑空间信息属性集 |
| 空间使用特征 | 文字 | 单体 | IfcSpace(空间) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑空间信息属性集 |
| 是否为室外停车场 | 布尔 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集 |
| 停车位类别 | 文字 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集 |
| 停车位位置信息 | 文字 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集 |
| 停车位类型 | 文字 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集 |
| 层数 | 整数 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集、Pset_室内停车场信息属性集 |
| 车位数 | 整数 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集 |
| 充电车位 | 整数 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集 |
| 公建车位 | 整数 | 单体 | IfcSpace(空间)、IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车位信息属性集、Pset_停车场信息属性集 |
| 楼(地)面反射比 | 数值 | 单体 | IfcSpace(空间) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_居住建筑空间信息属性集 |
| 区域类别 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 区域标记 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 结构净高 | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 建筑净高 | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 结构层高 | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 计算标高(埋深) | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 面积 | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 楼层数量 | 整数 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 是否为疏散分区 | 布尔 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 区域人数 | 整数 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 户型名 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 户型标识 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 居室个数 | 整数 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 区域长度 | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 区域宽度 | 数值 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 功能名称 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 轮廓线 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 疏散区域 | 布尔 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 区域位置 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 |
| 机动车位数量 | 整数 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 |
| 非机动车位数量 | 整数 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 |
| 无障碍车位数量 | 整数 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 |
| 车场类型 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 |
| 是否为充电车场 | 布尔 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 |
| 公建大类 | 文字 | 单体 | IfcSpatialZone(区域)、IfcSlab(板类) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_室内停车场信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 公建小类 | 文字 | 单体 | IfcSpatialZone(区域)、IfcSlab(板类) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、楼板(OST_Floors) | Pset_室内停车场信息属性集、Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集、Pset_楼顶间信息属性集 |
| 停车类型 | 文字 | 单体 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_室内停车场信息属性集 |
| 间距审查 | 布尔 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集 |
| 退让审查 | 布尔 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集、Pset_雨篷信息属性集、Pset_空调板信息属性集、Pset_飘窗信息属性集、Pset_架空信息属性集 |
| 进深 | 数值 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集 |
| 是否有顶 | 布尔 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集 |
| 是否为露台 | 布尔 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集 |
| 是否封闭 | 布尔 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集 |
| 标高 | 数值 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_阳台信息属性集 |
| 屋顶类型 | 文字 | 单体 | IfcSlab(板类) | 楼板(OST_Floors) | Pset_飘窗信息属性集 |

## 最小报建参数核对表

| 参数名 | 类型 | 分类 | IFC 构件 | Revit 绑定构件 | 来源 Pset | 匹配方式 |
| --- | --- | --- | --- | --- | --- | --- |
| 基点坐标X | 数值 | 全局、最小报建 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 | 同 Pset 同名 |
| 基点坐标Y | 数值 | 全局、最小报建 | IfcProject(项目) | 项目信息(OST_ProjectInformation) | Pset_申报信息属性集 | 同 Pset 同名 |
| 区内道路类型 | 文字 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_道路边界线信息属性集 | 同 Pset 同名 |
| 道路宽度 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_道路边界线信息属性集 | 同 Pset 同名 |
| 类型 | 按名称推断 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_绿地信息属性集、Pset_规划净用地信息属性集、Pset_规划总用地信息属性集 | 继承同 Pset IFC 映射 |
| 投影面积 | 数值 | 总图、最小报建、单体 | IfcSite(场地)、IfcSpatialZone(区域)、IfcBuilding(建筑/楼栋) | 场地(OST_Site)、房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas)、项目信息(OST_ProjectInformation) | Pset_绿地信息属性集、Pset_规划净用地信息属性集、Pset_规划总用地信息属性集、Pset_停车场信息属性集、Pset_建筑技术信息属性集、Pset_建筑区域信息属性集 | 同 Pset 同名 |
| 绿地类型 | 文字 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_绿地信息属性集 | 同 Pset 同名 |
| 折算系数 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_绿地信息属性集 | 同 Pset 同名 |
| 名称 | 文字 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_规划净用地信息属性集、Pset_规划总用地信息属性集 | 同 Pset 同名 |
| 版本 | 文字 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_规划净用地信息属性集 | 同 Pset 同名 |
| 机动车位数量 | 整数 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 | 同 Pset 同名 |
| 非机动车位数量 | 整数 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 | 同 Pset 同名 |
| 无障碍车位数量 | 整数 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_停车场信息属性集 | 同 Pset 同名 |
| 长度 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 宽度 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 总用地面积 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 可建设用地面积 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 建筑密度 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 建筑比例 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 混合用地比例 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 容积率 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 绿地率 | 数值 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 地块编号 | 文字 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 城市用地分类和使用 | 文字 | 总图、最小报建 | IfcSite(场地) | 场地(OST_Site) | Pset_场地信息属性集 | 同 Pset 同名 |
| 建筑编号 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 规划高度 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 地上建筑层数 | 整数 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 地下建筑层数 | 整数 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 保障房户数 | 整数 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑类型名称 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑用途名称 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑类别 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 消防高度 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 檐口高度 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 屋脊高度 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 海拔高度 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 临时永久 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 结构类型 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 形状 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 山墙开窗 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 民用建筑 | 布尔 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑名称 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 地坪标高 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 耐火等级 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 安全等级 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 户数 | 整数 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑状态 | 文字 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑面积 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 计容面积 | 按名称推断 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 继承同 Pset IFC 映射 |
| 建筑体积 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 人数 | 整数 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 地下建筑高度 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 室内外高差最大值 | 数值 | 单体、最小报建 | IfcBuilding(建筑/楼栋) | 项目信息(OST_ProjectInformation) | Pset_建筑技术信息属性集 | 同 Pset 同名 |
| 建筑层高 | 数值 | 单体、最小报建 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 | 同 Pset 同名 |
| 楼层名称 | 文字 | 单体、最小报建 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 | 同 Pset 同名 |
| 建筑基底 | 数值 | 单体、最小报建 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 | 同 Pset 同名 |
| 底标高 | 数值 | 单体、最小报建 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 | 同 Pset 同名 |
| 计算标高 | 数值 | 单体、最小报建 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 | 同 Pset 同名 |
| 楼层面积 | 数值 | 单体、最小报建 | IfcBuildingStorey(楼层) | 标高(OST_Levels) | Pset_建筑楼层信息属性集 | 同 Pset 同名 |
| 主功能类别 | 文字 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 | 同 Pset 同名 |
| 子功能类别 | 文字 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 | 同 Pset 同名 |
| 计算系数 | 数值 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 | 同 Pset 同名 |
| 计容系数 | 数值 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 | 同 Pset 同名 |
| 构件类型 | 按名称推断 | 单体、最小报建 | IfcSpatialZone(区域) | 房间(OST_Rooms)、空间(OST_MEPSpaces)、面积(OST_Areas) | Pset_建筑区域信息属性集 | 继承同 Pset IFC 映射 |
