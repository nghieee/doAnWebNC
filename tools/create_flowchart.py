"""
Tạo sơ đồ quy trình nghiệp vụ Bán hàng trực tuyến
Sử dụng thư viện graphviz
"""

from graphviz import Digraph

# ===============================
# CÁCH 1: Sơ đồ BPMN (Khuyến nghị)
# ===============================

def create_bpmn_flowchart():
    """Tạo sơ đồ BPMN cho quy trình bán hàng"""
    
    dot = Digraph('BPMN_Sales_Process', format='png')
    dot.attr(rankdir='TB', 
             size='10,12',
             dpi='150',
             fontname='Times New Roman',
             fontsize='12')
    
    # Màu sắc theo chuẩn BPMN
    dot.attr('node', fontname='Times New Roman', fontsize='11')
    
    # Pool/Border cho toàn bộ quy trình
    dot.attr('graph', 
             label='Sơ đồ 1.1: Quy trình Quản lý Bán hàng trực tuyến\n(Nhà Thuốc Long Châu)',
             labelloc='t',
             fontsize='14',
             fontname='Times New Roman Bold',
             bgcolor='white',
             pad='0.5',
             nodesep='0.6',
             ranksep='0.8')
    
    # ============== KHỞI ĐẦU ==============
    dot.node('start', 'Bắt đầu', 
             shape='ellipse', 
             style='filled', 
             fillcolor='#2E7D32',  # Xanh lá đậm
             fontcolor='white',
             width='1.5')
    
    # ============== CÁC BƯỚC CHÍNH ==============
    
    # Bước 1: Khách hàng đặt mua
    dot.node('step1', '1. Khách hàng đặt mua sản phẩm\n─────────────────\n• Truy cập website\n• Tìm kiếm sản phẩm\n• Thêm vào giỏ hàng\n• Cung cấp thông tin giao hàng',
             shape='box',
             style='rounded,filled',
             fillcolor='#E3F2FD',  # Xanh nhạt
             width='3')
    
    # Bước 2: Kiểm tra tồn kho
    dot.node('step2', '2. Kiểm tra tồn kho\n─────────────────\n• Hệ thống kiểm tra số lượng tồn\n• Đủ hàng → Chuyển bước tiếp\n• Không đủ → Thông báo cho KH',
             shape='diamond',
             style='filled',
             fillcolor='#FFF3E0',  # Cam nhạt
             width='2.5')
    
    # Bước 3: Lập hóa đơn
    dot.node('step3', '3. Lập Hóa đơn giao dịch\n─────────────────\n• Nhân viên xác nhận đơn hàng\n• Tạo HÓA ĐƠN: mã, ngày, KH\n• Tạo CHI TIẾT: SP, SL, đơn giá',
             shape='box',
             style='rounded,filled',
             fillcolor='#E8F5E9',  # Xanh lá nhạt
             width='3')
    
    # Bước 4: Cập nhật tồn kho
    dot.node('step4', '4. Cập nhật tồn kho\n─────────────────\n• Trừ giảm số lượng tồn\n• Đảm bảo tính nhất quán dữ liệu',
             shape='box',
             style='rounded,filled',
             fillcolor='#F3E5F5',  # Tím nhạt
             width='3')
    
    # Bước 5: Giao hàng
    dot.node('step5', '5. Giao hàng cho khách\n─────────────────\n• Chuyển cho đơn vị vận chuyển\n• Khách nhận hàng',
             shape='box',
             style='rounded,filled',
             fillcolor='#E0F7FA',  # Xanh dương nhạt
             width='3')
    
    # Bước 6: Thanh toán
    dot.node('step6', '6. Thanh toán & Hoàn tất\n─────────────────\n• Khách thanh toán\n• Cập nhật trạng thái "Đã thanh toán"\n• Hoàn tất giao dịch',
             shape='box',
             style='rounded,filled',
             fillcolor='#FFEBEE',  # Đỏ nhạt
             width='3')
    
    # ============== KẾT THÚC ==============
    dot.node('end', 'Kết thúc', 
             shape='ellipse', 
             style='filled', 
             fillcolor='#C62828',  # Đỏ đậm
             fontcolor='white',
             width='1.5')
    
    # ============== LUỒNG XỬ LÝ ==============
    
    # Bắt đầu → Bước 1
    dot.edge('start', 'step1', color='#1565C0', penwidth='2')
    
    # Bước 1 → Bước 2
    dot.edge('step1', 'step2', color='#1565C0', penwidth='2', label='Đặt hàng')
    
    # Bước 2 → Bước 3 (Đủ hàng)
    dot.edge('step2', 'step3', color='#2E7D32', penwidth='2', 
             label='  Đủ hàng', fontcolor='#2E7D32')
    
    # Bước 2 → Thông báo (Không đủ)
    dot.node('notify', 'Thông báo\nkhông đủ hàng',
             shape='box',
             style='filled,rounded',
             fillcolor='#FFCCBC',
             width='1.5')
    
    dot.edge('step2', 'notify', color='#E65100', penwidth='2', 
             label='  Không đủ', fontcolor='#E65100')
    
    # Thông báo → Kết thúc
    dot.edge('notify', 'end', color='#E65100', penwidth='2', style='dashed')
    
    # Bước 3 → Bước 4
    dot.edge('step3', 'step4', color='#1565C0', penwidth='2', 
             label='  Xác nhận', fontcolor='#1565C0')
    
    # Bước 4 → Bước 5
    dot.edge('step4', 'step5', color='#1565C0', penwidth='2', 
             label='  Hoàn tất', fontcolor='#1565C0')
    
    # Bước 5 → Bước 6
    dot.edge('step5', 'step6', color='#1565C0', penwidth='2', 
             label='  Nhận hàng', fontcolor='#1565C0')
    
    # Bước 6 → Kết thúc
    dot.edge('step6', 'end', color='#1565C0', penwidth='2', 
             label='  Thanh toán', fontcolor='#1565C0')
    
    # Lưu sơ đồ
    dot.render('docs/CHQTCSDL/SoDoBPMN_QuyTrinhBanHang', cleanup=True)
    print("✅ Đã tạo: SoDoBPMN_QuyTrinhBanHang.png")
    
    return dot


# ===============================
# CÁCH 2: Sơ đồ Swimlane (Chi tiết hơn)
# ===============================

def create_swimlane_diagram():
    """Tạo sơ đồ Swimlane cho quy trình bán hàng"""
    
    dot = Digraph('Swimlane_Sales', format='png')
    dot.attr(rankdir='TB', 
             size='14,10',
             dpi='150',
             fontname='Times New Roman',
             fontsize='11',
             compound='true',
             newrank='true')
    
    dot.attr('graph',
             label='Sơ đồ 1.2: Sơ đồ Swimlane - Quy trình Bán hàng',
             labelloc='t',
             fontsize='14',
             fontname='Times New Roman Bold',
             bgcolor='white',
             pad='0.5')
    
    # ====== SUBGRAPHS cho Swimlane ======
    
    # Swimlane Khách hàng
    with dot.subgraph(name='cluster_customer') as c:
        c.attr(label='Khách hàng', 
               style='filled,rounded',
               fillcolor='#E3F2FD',
               fontname='Times New Roman Bold')
        c.node('c_search', 'Tìm kiếm sản phẩm', shape='box', style='filled,rounded')
        c.node('c_cart', 'Thêm vào giỏ hàng', shape='box', style='filled,rounded')
        c.node('c_checkout', 'Đặt hàng', shape='box', style='filled,rounded')
        c.node('c_receive', 'Nhận hàng', shape='box', style='filled,rounded')
        c.node('c_pay', 'Thanh toán', shape='box', style='filled,rounded')
    
    # Swimlane Hệ thống
    with dot.subgraph(name='cluster_system') as c:
        c.attr(label='Hệ thống (Oracle DB)', 
               style='filled,rounded',
               fillcolor='#E8F5E9',
               fontname='Times New Roman Bold')
        c.node('s_check', 'Kiểm tra tồn kho', shape='box', style='filled,rounded')
        c.node('s_create', 'Tạo Hóa đơn', shape='box', style='filled,rounded')
        c.node('s_update', 'Cập nhật tồn kho', shape='box', style='filled,rounded')
    
    # Swimlane Nhân viên
    with dot.subgraph(name='cluster_staff') as c:
        c.attr(label='Nhân viên Kinh doanh', 
               style='filled,rounded',
               fillcolor='#FFF3E0',
               fontname='Times New Roman Bold')
        c.node('e_confirm', 'Xác nhận đơn hàng', shape='box', style='filled,rounded')
    
    # Swimlane Giao hàng
    with dot.subgraph(name='cluster_delivery') as c:
        c.attr(label='Đơn vị vận chuyển', 
               style='filled,rounded',
               fillcolor='#F3E5F5',
               fontname='Times New Roman Bold')
        c.node('d_ship', 'Giao hàng', shape='box', style='filled,rounded')
    
    # Kết nối
    dot.edge('c_search', 'c_cart')
    dot.edge('c_cart', 'c_checkout')
    dot.edge('c_checkout', 's_check')
    dot.edge('s_check', 'e_confirm', label='Đủ hàng')
    dot.edge('e_confirm', 's_create')
    dot.edge('s_create', 's_update')
    dot.edge('s_update', 'd_ship')
    dot.edge('d_ship', 'c_receive')
    dot.edge('c_receive', 'c_pay')
    dot.edge('c_pay', 's_create', style='dashed', label='Xác nhận TT')
    
    dot.render('docs/CHQTCSDL/SoDoSwimlane_QuyTrinhBanHang', cleanup=True)
    print("✅ Đã tạo: SoDoSwimlane_QuyTrinhBanHang.png")
    
    return dot


# ===============================
# CÁCH 3: Sơ đồ chuẩn (Simple Flowchart - ASCII style)
# ===============================

def create_simple_flowchart():
    """Tạo sơ đồ đơn giản dạng Flowchart"""
    
    dot = Digraph('Simple_Flowchart', format='png')
    dot.attr(rankdir='TB',
             size='8,10',
             dpi='150',
             fontname='Times New Roman',
             fontsize='11')
    
    dot.attr('graph',
             label='Sơ đồ 1.1: Quy trình Quản lý Bán hàng trực tuyến',
             labelloc='t',
             fontsize='13',
             fontname='Times New Roman Bold',
             bgcolor='white',
             pad='0.5')
    
    # Nodes
    dot.node('start', 'BẮT ĐẦU', shape='ellipse', style='filled', 
             fillcolor='#2E7D32', fontcolor='white', width='1.3')
    
    dot.node('step1', '1. Khách hàng đặt mua sản phẩm', shape='box', 
             style='filled,rounded', fillcolor='#BBDEFB')
    
    dot.node('step2', '2. Kiểm tra tồn kho?', shape='diamond', 
             style='filled', fillcolor='#FFE082', width='2')
    
    dot.node('step3', '3. Lập Hóa đơn giao dịch', shape='box', 
             style='filled,rounded', fillcolor='#C8E6C9')
    
    dot.node('step4', '4. Cập nhật tồn kho', shape='box', 
             style='filled,rounded', fillcolor='#E1BEE7')
    
    dot.node('step5', '5. Giao hàng cho khách', shape='box', 
             style='filled,rounded', fillcolor='#B2EBF2')
    
    dot.node('step6', '6. Thanh toán & Hoàn tất', shape='box', 
             style='filled,rounded', fillcolor='#FFCDD2')
    
    dot.node('end', 'KẾT THÚC', shape='ellipse', style='filled', 
             fillcolor='#C62828', fontcolor='white', width='1.3')
    
    dot.node('fail', 'Thông báo\nKhông đủ hàng', shape='box', 
             style='filled,rounded', fillcolor='#FFCCBC')
    
    # Edges
    dot.edge('start', 'step1', penwidth='2')
    dot.edge('step1', 'step2', penwidth='2')
    dot.edge('step2', 'step3', label='  Đủ hàng', penwidth='2', color='#2E7D32', fontcolor='#2E7D32')
    dot.edge('step2', 'fail', label='  Không đủ', penwidth='2', color='#E65100', fontcolor='#E65100')
    dot.edge('fail', 'end', penwidth='1', style='dashed')
    dot.edge('step3', 'step4', penwidth='2')
    dot.edge('step4', 'step5', penwidth='2')
    dot.edge('step5', 'step6', penwidth='2')
    dot.edge('step6', 'end', penwidth='2')
    
    dot.render('docs/CHQTCSDL/SoDoFlowchart_QuyTrinhBanHang', cleanup=True)
    print("✅ Đã tạo: SoDoFlowchart_QuyTrinhBanHang.png")
    
    return dot


# ===============================
# CHẠY TẤT CẢ
# ===============================

if __name__ == '__main__':
    print("🎯 Đang tạo sơ đồ quy trình nghiệp vụ...")
    print("=" * 50)
    
    # Tạo các sơ đồ
    create_bpmn_flowchart()
    create_swimlane_diagram()
    create_simple_flowchart()
    
    print("=" * 50)
    print("✅ Hoàn thành! Các file đã được lưu trong thư mục docs/CHQTCSDL/")
    print("\n📁 Các file tạo ra:")
    print("   1. SoDoBPMN_QuyTrinhBanHang.png")
    print("   2. SoDoSwimlane_QuyTrinhBanHang.png")
    print("   3. SoDoFlowchart_QuyTrinhBanHang.png")
