import{Ga as w,Jb as E,Ka as f,Kb as T,Lb as N,Q as S,Qb as D,Sb as j,Ta as m,Va as $,Vb as L,W as I,Wa as _,Xa as y,Ya as a,Za as n,bb as c,ib as o,jb as r,kb as p,lb as P,tb as v,ub as g,ya as l}from"./chunk-GYX2CZ7X.js";var M=(i,t)=>t.productId;function O(i,t){if(i&1&&(a(0,"div"),o(1),n()),i&2){let e=c();l(),r(e.store==null?null:e.store.address)}}function Q(i,t){if(i&1&&o(0),i&2){let e=c(2);p(" Tel: ",e.store==null?null:e.store.phone," ")}}function V(i,t){i&1&&o(0," \xB7 ")}function H(i,t){if(i&1&&o(0),i&2){let e=c(2);p(" ",e.store==null?null:e.store.email," ")}}function U(i,t){if(i&1&&(a(0,"div"),f(1,Q,1,1)(2,V,1,0)(3,H,1,1),n()),i&2){let e=c();l(),m(e.store!=null&&e.store.phone?1:-1),l(),m(e.store!=null&&e.store.phone&&(e.store!=null&&e.store.email)?2:-1),l(),m(e.store!=null&&e.store.email?3:-1)}}function G(i,t){if(i&1&&o(0),i&2){let e=c(2);p(" NTN # ",e.store==null?null:e.store.ntn," ")}}function W(i,t){if(i&1&&o(0),i&2){let e=c(2);p(" \xA0\xA0STRN # ",e.store==null?null:e.store.strn," ")}}function Y(i,t){if(i&1&&(a(0,"div"),f(1,G,1,1)(2,W,1,1),n()),i&2){let e=c();l(),m(e.store!=null&&e.store.ntn?1:-1),l(),m(e.store!=null&&e.store.strn?2:-1)}}function J(i,t){if(i&1&&(a(0,"div"),o(1),n()),i&2){let e=c();l(),p("Cashier: ",e.cashierName,"")}}function K(i,t){if(i&1&&(a(0,"div"),o(1),n()),i&2){let e=c();l(),p("Sales Person: ",e.salesPersonName,"")}}function X(i,t){if(i&1&&(a(0,"tr")(1,"td",17),o(2),n(),a(3,"td",18),o(4),n(),a(5,"td",19),o(6),v(7,"number"),n(),a(8,"td",17),o(9),n(),a(10,"td",19),o(11),v(12,"number"),n(),a(13,"td",19),o(14),v(15,"number"),n()()),i&2){let e=t.$implicit,s=t.$index,x=c(2);l(2),r(s+1),l(2),r(e.productName),l(2),r(g(7,6,e.unitPrice,"1.2-2")),l(3),r(e.quantity),l(2),r(g(12,9,x.lineDiscount(e),"1.2-2")),l(3),r(g(15,12,e.lineTotal,"1.2-2"))}}function Z(i,t){if(i&1&&(a(0,"div",15),o(1),n()),i&2){let e=t.$implicit,s=t.$index;l(),P("",s+1,". ",e,"")}}function ee(i,t){if(i&1&&(a(0,"div",0)(1,"div",1)(2,"div",2),o(3,"Purchase Slip"),n(),a(4,"div",3),o(5),n(),f(6,O,2,1,"div")(7,U,4,3,"div")(8,Y,3,2,"div"),n(),a(9,"div",4)(10,"div"),o(11,"Invoice: "),a(12,"strong"),o(13),n()(),a(14,"div"),o(15),n(),a(16,"div"),o(17),v(18,"date"),n(),f(19,J,2,1,"div"),a(20,"div"),o(21),n(),f(22,K,2,1,"div"),n(),a(23,"table",5)(24,"thead")(25,"tr",6)(26,"th",7),o(27,"Sr"),n(),a(28,"th",8),o(29,"Product"),n(),a(30,"th",9),o(31,"Price"),n(),a(32,"th",7),o(33,"Qty"),n(),a(34,"th",9),o(35,"Disc"),n(),a(36,"th",9),o(37,"Total"),n()()(),a(38,"tbody"),_(39,X,16,15,"tr",null,M),n()(),a(41,"div",10)(42,"span"),o(43),n(),a(44,"span"),o(45),v(46,"number"),n()(),a(47,"div",11)(48,"div",12)(49,"span"),o(50,"Value Excluding Sales Tax"),n(),a(51,"span"),o(52),v(53,"number"),n()(),a(54,"div",12)(55,"span"),o(56),n(),a(57,"span"),o(58),v(59,"number"),n()(),a(60,"div",12)(61,"span"),o(62,"Sales Tax"),n(),a(63,"span"),o(64),v(65,"number"),n()(),a(66,"div",13)(67,"span"),o(68,"Net Total (Incl. Sales Tax)"),n(),a(69,"span"),o(70),v(71,"number"),n()()(),a(72,"div",14)(73,"div",3),o(74,"Refund & Exchange Policy:"),n(),_(75,Z,2,2,"div",15,$),n(),a(77,"div",16),o(78," Thanks for shopping with us! "),n()()),i&2){let e=t,s=c();l(5),r(e.warehouseName),l(),m(e.store!=null&&e.store.address?6:-1),l(),m(e.store!=null&&e.store.phone||e.store!=null&&e.store.email?7:-1),l(),m(e.store!=null&&e.store.ntn||e.store!=null&&e.store.strn?8:-1),l(5),r(e.orderNumber),l(2),p("MOP: ",e.paymentMethod,""),l(2),p("Date: ",g(18,17,e.createdAtUtc,"medium"),""),l(2),m(e.cashierName?19:-1),l(2),p("Customer: ",e.customerName||"Walk-in",""),l(),m(e.salesPersonName?22:-1),l(17),y(e.lines),l(4),p("Gross Total (",s.totalQuantity,")"),l(2),r(g(46,20,e.subtotal,"1.2-2")),l(7),r(g(53,23,s.taxableValue,"1.2-2")),l(4),p("Discount",e.discountLabel?" ("+e.discountLabel+")":"",""),l(2),p("-",g(59,26,e.discountAmount,"1.2-2"),""),l(6),r(g(65,29,e.taxAmount,"1.2-2")),l(6),r(g(71,32,e.total,"1.2-2")),l(5),y(s.policyLines)}}var A=["Goods once sold may be exchanged within 15 days of purchase.","Items must be unused, unwashed, and in their original packing.","This receipt is required for any exchange or claim.","Sale/clearance-priced items are not eligible for exchange."],B=class i{sale=null;defaultPolicyLines=A;lineDiscount(t){return b(t.unitPrice*t.quantity*t.discountPercent/100)}get taxableValue(){return this.sale?b(this.sale.subtotal-this.sale.discountAmount):0}get totalQuantity(){return this.sale?.lines.reduce((t,e)=>t+e.quantity,0)??0}get policyLines(){return F(this.sale?.store?.footerText)}toPrintableHtml(){if(!this.sale)return"";let t=this.sale,e=t.store,s=t.lines.map((u,h)=>`
          <tr>
            <td style="padding:3px 0;text-align:center">${h+1}</td>
            <td style="padding:3px 0">${d(u.productName)}</td>
            <td style="padding:3px 0;text-align:right">${u.unitPrice.toFixed(2)}</td>
            <td style="padding:3px 0;text-align:center">${u.quantity}</td>
            <td style="padding:3px 0;text-align:right">${b(u.unitPrice*u.quantity*u.discountPercent/100).toFixed(2)}</td>
            <td style="padding:3px 0;text-align:right">${u.lineTotal.toFixed(2)}</td>
          </tr>`).join(""),x=t.lines.reduce((u,h)=>u+h.quantity,0),q=b(t.subtotal-t.discountAmount),R=F(e?.footerText).map((u,h)=>`<div style="margin-top:2px">${h+1}. ${d(u)}</div>`).join("");return`
      <div style="font-family:'Courier New',monospace;width:300px;margin:0 auto;font-size:11px;color:#111">
        <div style="text-align:center;margin-bottom:8px">
          <div style="font-size:15px;font-weight:700">Purchase Slip</div>
          <div style="font-weight:700">${d(t.warehouseName)}</div>
          ${e?.address?`<div>${d(e.address)}</div>`:""}
          ${e?.phone||e?.email?`<div>${e.phone?`Tel: ${d(e.phone)}`:""}${e.phone&&e.email?" &middot; ":""}${e.email?d(e.email):""}</div>`:""}
          ${e?.ntn||e?.strn?`<div>${e.ntn?`NTN # ${d(e.ntn)}`:""}${e.ntn&&e.strn?"  ":""}${e.strn?`STRN # ${d(e.strn)}`:""}</div>`:""}
        </div>
        <div style="border-top:1px dashed #000;border-bottom:1px dashed #000;padding:6px 0;margin-bottom:6px;display:grid;grid-template-columns:1fr 1fr;gap:2px 8px">
          <div>Invoice: <strong>${d(t.orderNumber)}</strong></div>
          <div>MOP: ${d(t.paymentMethod)}</div>
          <div>Date: ${new Date(t.createdAtUtc).toLocaleString()}</div>
          ${t.cashierName?`<div>Cashier: ${d(t.cashierName)}</div>`:"<div></div>"}
          <div>Customer: ${d(t.customerName??"Walk-in")}</div>
          ${t.salesPersonName?`<div>Sales Person: ${d(t.salesPersonName)}</div>`:""}
        </div>
        <table style="width:100%;border-collapse:collapse">
          <thead>
            <tr style="border-bottom:1px solid #000">
              <th style="text-align:center;padding-bottom:4px">Sr</th>
              <th style="text-align:left;padding-bottom:4px">Product</th>
              <th style="text-align:right;padding-bottom:4px">Price</th>
              <th style="text-align:center;padding-bottom:4px">Qty</th>
              <th style="text-align:right;padding-bottom:4px">Disc</th>
              <th style="text-align:right;padding-bottom:4px">Total</th>
            </tr>
          </thead>
          <tbody>${s}</tbody>
        </table>
        <div style="border-top:1px solid #000;margin-top:4px;padding-top:4px;display:flex;justify-content:space-between;font-weight:700">
          <span>Gross Total (${x})</span><span>${t.subtotal.toFixed(2)}</span>
        </div>
        <div style="border-top:1px dashed #000;margin-top:6px;padding-top:6px">
          <div style="display:flex;justify-content:space-between"><span>Value Excluding Sales Tax</span><span>${q.toFixed(2)}</span></div>
          <div style="display:flex;justify-content:space-between"><span>Discount${t.discountLabel?` (${d(t.discountLabel)})`:""}</span><span>-${t.discountAmount.toFixed(2)}</span></div>
          <div style="display:flex;justify-content:space-between"><span>Sales Tax</span><span>${t.taxAmount.toFixed(2)}</span></div>
          <div style="display:flex;justify-content:space-between;font-weight:700;font-size:13px;border-top:1px solid #000;margin-top:4px;padding-top:4px">
            <span>Net Total (Incl. Sales Tax)</span><span>${t.total.toFixed(2)}</span>
          </div>
        </div>
        <div style="border-top:1px dashed #000;margin-top:8px;padding-top:6px;font-size:10px">
          <div style="font-weight:700">Refund &amp; Exchange Policy:</div>
          ${R}
        </div>
        <div style="text-align:center;margin-top:10px;border-top:1px dashed #000;padding-top:6px">
          Thanks for shopping with us!
        </div>
      </div>
    `}static \u0275fac=function(e){return new(e||i)};static \u0275cmp=w({type:i,selectors:[["app-invoice-preview"]],inputs:{sale:"sale"},decls:1,vars:1,consts:[[2,"font-family","'Courier New', monospace","width","300px","margin","0 auto","font-size","11px","color","#111","background","white","padding","1rem","border-radius","var(--radius-md)","border","1px solid var(--border)"],[2,"text-align","center","margin-bottom","0.5rem"],[2,"font-size","1.05rem","font-weight","700"],[2,"font-weight","700"],[2,"border-top","1px dashed #000","border-bottom","1px dashed #000","padding","0.4rem 0","margin-bottom","0.4rem","display","grid","grid-template-columns","1fr 1fr","gap","0.1rem 0.5rem"],[2,"width","100%","border-collapse","collapse"],[2,"border-bottom","1px solid #000"],[2,"text-align","center","padding-bottom","0.25rem"],[2,"text-align","left","padding-bottom","0.25rem"],[2,"text-align","right","padding-bottom","0.25rem"],[2,"border-top","1px solid #000","margin-top","0.25rem","padding-top","0.25rem","display","flex","justify-content","space-between","font-weight","700"],[2,"border-top","1px dashed #000","margin-top","0.4rem","padding-top","0.4rem"],[2,"display","flex","justify-content","space-between"],[2,"display","flex","justify-content","space-between","font-weight","700","font-size","0.95rem","border-top","1px solid #000","margin-top","0.25rem","padding-top","0.25rem"],[2,"border-top","1px dashed #000","margin-top","0.6rem","padding-top","0.4rem","font-size","0.68rem"],[2,"margin-top","0.1rem"],[2,"text-align","center","margin-top","0.6rem","border-top","1px dashed #000","padding-top","0.4rem"],[2,"padding","0.2rem 0","text-align","center"],[2,"padding","0.2rem 0"],[2,"padding","0.2rem 0","text-align","right"]],template:function(e,s){if(e&1&&f(0,ee,79,35,"div",0),e&2){let x;m((x=s.sale)?0:-1,x)}},dependencies:[N,T,E],encapsulation:2})};function F(i){return!i||i.trim().length===0?A:i.split(`
`).map(t=>t.trim().replace(/^\d+[.)]\s*/,"")).filter(t=>t.length>0)}function b(i){return Math.round(i*100)/100}function d(i){return i.replace(/[&<>"']/g,t=>({"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;","'":"&#39;"})[t])}var C=`${L.apiBaseUrl}/sales`,k=class i{http=I(j);list(t){let e=new D;for(let[s,x]of Object.entries(t))x!=null&&x!==""&&(e=e.set(s,String(x)));return this.http.get(C,{params:e})}create(t){return this.http.post(C,t)}get(t){return this.http.get(`${C}/${t}`)}static \u0275fac=function(e){return new(e||i)};static \u0275prov=S({token:i,factory:i.\u0275fac,providedIn:"root"})};export{B as a,k as b};
